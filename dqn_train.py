import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np
import random
from collections import deque
from signalrcore.hub_connection_builder import HubConnectionBuilder
import json
import time
import matplotlib.pyplot as plt
import logging
import os
import pickle

# === 优先经验回放（加了 save/load 方法） ===
class PrioritizedReplayBuffer:
    def __init__(self, capacity, alpha=0.6):
        self.capacity = capacity
        self.buffer = []
        self.pos = 0
        self.priorities = np.zeros((capacity,), dtype=np.float32)
        self.alpha = alpha

    def __len__(self):
        return len(self.buffer)

    def push(self, state, action, reward, next_state, done):
        max_priority = self.priorities.max() if self.buffer else 1.0
        if len(self.buffer) < self.capacity:
            self.buffer.append((state, action, reward, next_state, done))
        else:
            self.buffer[self.pos] = (state, action, reward, next_state, done)
        self.priorities[self.pos] = max_priority
        self.pos = (self.pos + 1) % self.capacity

    def sample(self, batch_size, beta=0.4):
        priorities = self.priorities if len(self.buffer) == self.capacity else self.priorities[:self.pos]
        probs = priorities ** self.alpha
        probs /= probs.sum()
        indices = np.random.choice(len(self.buffer), batch_size, p=probs)
        samples = [self.buffer[idx] for idx in indices]
        total = len(self.buffer)
        weights = (total * probs[indices]) ** (-beta)
        weights /= weights.max()
        weights = np.array(weights, dtype=np.float32)
        states, actions, rewards, next_states, dones = zip(*samples)
        return (
            torch.FloatTensor(np.array(states)),
            torch.LongTensor(actions).unsqueeze(1),
            torch.FloatTensor(rewards).unsqueeze(1),
            torch.FloatTensor(np.array(next_states)),
            torch.FloatTensor(dones).unsqueeze(1),
            torch.FloatTensor(weights).unsqueeze(1),
            indices
        )

    def update_priorities(self, indices, priorities):
        for idx, priority in zip(indices, priorities):
            self.priorities[idx] = priority

    def save(self, filename):
        with open(filename, 'wb') as f:
            pickle.dump((self.buffer, self.pos, self.priorities), f)

    def load(self, filename):
        if os.path.exists(filename):
            with open(filename, 'rb') as f:
                self.buffer, self.pos, self.priorities = pickle.load(f)

#环境、网络结构、预训练代码
class CSharpEnv:
    def __init__(self, url="http://localhost:5000/gamehub"):
        self.state = None
        self.expert_action = None
        self.done = False
        self.reward = 0

        self.hub_connection = HubConnectionBuilder()\
            .with_url(url)\
            .configure_logging(logging.INFO)\
            .build()

        self.hub_connection.on("ReceiveState", self._on_receive_state)
        self.hub_connection.on("ReceiveStep", self._on_receive_step)
        self.hub_connection.on("ReceiveExpertAction", self._on_receive_expert_action)

        self.hub_connection.on_error(lambda data: print(f"[SignalR Error] {data}"))

        self.hub_connection.start()
        time.sleep(1)

    def _merge_state(self, board, mines, numbers):
        board = np.array(board).reshape(15, 15)
        mines = np.array(mines).reshape(15, 15)
        numbers = np.array(numbers).reshape(15, 15)
        return np.stack([board, mines, numbers], axis=0)

    def _on_receive_state(self, args):
        data = json.loads(args[0])
        self.state = self._merge_state(data["board"], data["mines"], data["numbers"])
        self.done = data["done"]

    def _on_receive_step(self, args):
        data = json.loads(args[0])
        self.state = self._merge_state(data["board"], data["mines"], data["numbers"])
        self.reward = data["reward"]
        self.done = data["done"]

    def _on_receive_expert_action(self, args):
        self.expert_action = int(args[0])

    def reset(self):
        self.state = None
        self.hub_connection.send("ResetGame", [])
        while self.state is None:
            time.sleep(0.05)
        return self.state

    def step(self, action,penalize_forbidden=False,is_agent_move=False):
        self.state = None
        self.hub_connection.send("StepGame", [int(action),penalize_forbidden,is_agent_move])
        while self.state is None:
            time.sleep(0.05)
        return self.state, self.reward, self.done

    def get_expert_action(self):
        self.expert_action = None
        self.hub_connection.send("GetExpertAction", [])
        while self.expert_action is None:
            time.sleep(0.01)
        return self.expert_action

    def get_valid_action_mask(self):
        board = self.state[0]
        mask = (board == 0).flatten()
        return mask.tolist()

    def close(self):
        self.hub_connection.stop()

class DQN(nn.Module):
    def __init__(self, input_channels, action_dim):
        super(DQN, self).__init__()
        self.net = nn.Sequential(
            nn.Flatten(),
            nn.Linear(input_channels * 15 * 15, 512),
            nn.ReLU(),
            nn.Linear(512, 256),
            nn.ReLU(),
            nn.Linear(256, action_dim)
        )

    def forward(self, x):
        return self.net(x)

def pretrain_with_expert_data(policy_net, optimizer, env, buffer, epochs=20000):
    print("开始专家数据预训练...")
    for epoch in range(epochs):
        state = env.reset()
        done = False
        turn = 0
        while not done:
            action = env.get_expert_action()
            next_state, reward, done = env.step(action, penalize_forbidden=False, is_agent_move=False)
            if turn%2==1:
                buffer.push(state, action, reward, next_state, done)

            # 训练 policy_net
            state_tensor = torch.FloatTensor(state).unsqueeze(0)
            with torch.no_grad():
                next_q = policy_net(torch.FloatTensor(next_state).unsqueeze(0)).max(1)[0].item()
                target_q_value = reward + 0.99 * next_q * (0 if done else 1)

            q_values = policy_net(torch.FloatTensor(state).unsqueeze(0))
            loss = nn.MSELoss()(q_values[0, action], torch.tensor(target_q_value))

            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

            state = next_state
            turn += 1
        if epoch % 10 == 0:
            print(f"Pretrain Epoch {epoch} done")
    print("专家预训练完成")



# === 训练入口 ===
def train_dqn():
    env = CSharpEnv()
    input_channels = 3
    action_dim = 225

    policy_net = DQN(input_channels, action_dim)
    target_net = DQN(input_channels, action_dim)
    distill_net = DQN(input_channels, action_dim)
    optimizer = optim.Adam(policy_net.parameters(), lr=1e-3)
    buffer = PrioritizedReplayBuffer(100000)

    target_net.load_state_dict(policy_net.state_dict())
    distill_net.load_state_dict(policy_net.state_dict())
    target_net.eval()

    batch_size = 128
    gamma = 0.99
    tau = 0.01

    epsilon = 0.5
    min_epsilon = 0.05
    epsilon_decay = 0.9999963
    fixed_epsilon_episodes = 100000
    max_episodes = 500000
    rewards_log = []

    start_episode = 0  # 初始从第0集开始

    # === 检查是否有断点 ===
    if os.path.exists("checkpoint.pth"):
        print("检测到断点，正在加载中...")
        checkpoint = torch.load("checkpoint.pth")
        policy_net.load_state_dict(checkpoint['policy_net'])
        target_net.load_state_dict(checkpoint['target_net'])
        distill_net.load_state_dict(checkpoint['distill_net'])
        optimizer.load_state_dict(checkpoint['optimizer'])
        epsilon = checkpoint['epsilon']
        rewards_log = checkpoint['rewards_log']
        start_episode = checkpoint['episode'] + 1
        buffer.load("replay_buffer.pkl")
        print(f"恢复训练从第 {start_episode} 集开始")

    # === 预训练（仅首次训练时执行） ===
    if start_episode == 0:
        pretrain_with_expert_data(policy_net, optimizer, env, buffer, epochs=20000)

    # === 图表初始化 ===
    plt.ion()
    fig, ax = plt.subplots()
    line, = ax.plot([], [], label='Episode Reward')
    ax.set_title("DQN Training Reward Trend")
    ax.set_xlabel("Episode")
    ax.set_ylabel("Total Reward")
    ax.grid(True)
    plt.show(block=False)

    distill_interval = 2000
    distill_loss_fn = nn.MSELoss()

    for episode in range(start_episode, max_episodes):
        state = env.reset()
        done = False
        total_reward = 0
        penalize_forbidden = episode >= 500
        turn = 0

        while not done:

            if turn % 2 == 0:
                action = env.get_expert_action()
                if done:
                    break
                next_state, reward, done = env.step(action, penalize_forbidden=False, is_agent_move=False)
                #buffer.push(state, action, reward, next_state, done)
                state = next_state
            else:
                if random.random() < epsilon:
                    valid_actions = [i for i, valid in enumerate(env.get_valid_action_mask()) if valid]
                    action = random.choice(valid_actions)
                else:
                    with torch.no_grad():
                        state_tensor = torch.FloatTensor(state).unsqueeze(0)
                        q_values = policy_net(state_tensor).squeeze()
                        valid_mask = torch.tensor(env.get_valid_action_mask(), dtype=torch.bool)
                        q_values[~valid_mask] = -float('inf')
                        action = q_values.argmax().item()

                if done:
                    break
                next_state, reward, done = env.step(action, penalize_forbidden=penalize_forbidden, is_agent_move=True)
                buffer.push(state, action, reward, next_state, done)
                state = next_state
                total_reward += reward

                if len(buffer) >= batch_size:
                    beta = min(1.0, 0.4 + episode * 0.0001)
                    states, actions, rewards_, next_states, dones, weights, indices = buffer.sample(batch_size, beta)

                    current_q = policy_net(states).gather(1, actions)
                    with torch.no_grad():
                        next_q = target_net(next_states).max(1)[0].unsqueeze(1)
                        target_q = rewards_ + gamma * next_q * (1 - dones)

                    td_errors = (current_q - target_q).squeeze().detach().cpu().numpy()
                    priorities = np.abs(td_errors) + 1e-5
                    buffer.update_priorities(indices, priorities)

                    loss = (weights * (current_q - target_q) ** 2).mean()
                    optimizer.zero_grad()
                    loss.backward()
                    optimizer.step()

                    for target_param, param in zip(target_net.parameters(), policy_net.parameters()):
                        target_param.data.copy_(tau * param.data + (1 - tau) * target_param.data)
            turn += 1

        # 蒸馏机制
        if episode > 0 and episode % distill_interval == 0:
            print(f"Episode {episode}: Performing policy distillation...")
            for _ in range(10):
                if len(buffer) >= batch_size:
                    states, _, _, _, _, _, _ = buffer.sample(batch_size)
                    with torch.no_grad():
                        target_q = distill_net(states)
                    pred_q = policy_net(states)
                    distill_loss = distill_loss_fn(pred_q, target_q)
                    optimizer.zero_grad()
                    distill_loss.backward()
                    optimizer.step()

        if episode % 2000 == 0:
            distill_net.load_state_dict(policy_net.state_dict())

        # 更新 epsilon
        if episode > fixed_epsilon_episodes:
            epsilon = max(min_epsilon, epsilon * epsilon_decay)

        rewards_log.append(total_reward)
        print(f"Episode {episode}, Total Reward: {total_reward:.2f}, Epsilon: {epsilon:.3f}")

        # 更新图表
        line.set_data(range(len(rewards_log)), rewards_log)
        ax.set_xlim(0, max(10, len(rewards_log)))
        ax.set_ylim(min(rewards_log) - 1, max(rewards_log) + 1)
        fig.canvas.draw()
        fig.canvas.flush_events()

        # === 保存断点 ===
        if episode % 100 == 0:
            torch.save({
                'episode': episode,
                'policy_net': policy_net.state_dict(),
                'target_net': target_net.state_dict(),
                'distill_net': distill_net.state_dict(),
                'optimizer': optimizer.state_dict(),
                'epsilon': epsilon,
                'rewards_log': rewards_log
            }, "checkpoint.pth")
            buffer.save("replay_buffer.pkl")
            print(f"✅ 已保存断点于 Episode {episode}")

    torch.save(policy_net.state_dict(), "dqn_model_final.pth")
    plt.ioff()
    plt.savefig("reward_trend.png")
    plt.show()
    print("🎉 训练完成")
    env.close()

if __name__ == "__main__":
    train_dqn()
