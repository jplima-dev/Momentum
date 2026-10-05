# Momentum

Momentum é um jogo 3D de plataforma com visual **low poly**, inspirado em jogos como **Super Mario 64** e **Super Bear Adventure**.

O foco principal do projeto é a **movimentação**, explorando velocidade, derrapagem, impulso e uso de momentum para realizar movimentos especiais.

## 🎮 Estado atual

O projeto está em uma fase inicial de protótipo, com o sistema básico de movimentação já funcionando.

### Movimentação

- Movimento em terceira pessoa usando **WASD**
- Movimento baseado na direção da câmera
- Rotação do personagem acompanhando a direção do movimento
- Corrida usando **Shift**
- Derrapagem ao soltar as teclas de movimento
- Controle da desaceleração da derrapagem

### Pulo

- Pulo normal usando **Espaço**
- Gravidade e controle de velocidade vertical
- Segundo pulo no ar usando o sistema de momentum
- O segundo pulo é executado pelo **botão esquerdo do mouse**, quando não há WASD pressionado
- O segundo pulo usa momentum para aumentar o impulso vertical

### Momentum

Momentum é um valor acumulável que representa o impulso disponível para movimentos especiais.

Atualmente ele pode ser obtido de duas formas:

- Derrapando após o movimento
- Durante a queda depois de um pulo

O momentum pode ser usado para:

- **Dash horizontal**
- **Segundo pulo impulsionado (DashCima)**

### Dash

- Executado com o **botão esquerdo do mouse**
- Usa o momentum acumulado
- A direção é definida pelo WASD e pela câmera
- Possui limite mínimo e máximo de momentum utilizado
- O dash perde velocidade gradualmente
- Colisões laterais podem interromper o impulso

## 🎬 Animações atuais

O Animator possui atualmente:

- `parado`
- `Walk`
- `Run`
- `Pular`
- `Dash`
- `DashCima`

Parâmetros utilizados:

- `Andando` — Bool
- `Correndo` — Bool
- `Pular` — Trigger
- `Dash` — Trigger
- `DashCima` — Trigger

## ✨ Efeitos visuais

Existe uma partícula (`ParticulaPulo`) utilizada nos movimentos especiais.

Atualmente ela é acionada quando:

- O personagem pula
- O personagem executa o segundo pulo
- O personagem executa um dash

## 📷 Câmera

O projeto utiliza uma câmera em terceira pessoa que acompanha o personagem e permite controlar sua orientação através do mouse.

## 🧱 Ambiente

O protótipo atual possui um cenário de testes com plataformas, paredes, desníveis e áreas para testar:

- Movimento
- Derrapagem
- Pulo
- Queda
- Momentum
- Dash
- Segundo pulo

## 🛠️ Tecnologia

- **Unity 6.3 LTS**
- Unity Input System
- C#
- Character Controller
- Animator
- Particle System

## 🚧 Próximos passos

O projeto ainda está em desenvolvimento. Algumas ideias para as próximas versões:

- Melhorar a sensação da movimentação
- Refinar o sistema de momentum
- Adicionar mais interações com o cenário
- Criar desafios focados em movimentação
- Melhorar animações e efeitos visuais
- Criar novas mecânicas baseadas em velocidade e impulso
- Desenvolver fases completas

## 📌 Histórico de versões

As versões do jogo são mantidas no GitHub para acompanhar a evolução do projeto e permitir o desenvolvimento em diferentes computadores.

### v0.1 — Protótipo de movimentação

- Movimentação em terceira pessoa
- Corrida
- Pulo
- Derrapagem
- Sistema de momentum
- Ganho de momentum por derrapagem e queda
- Dash horizontal
- Segundo pulo impulsionado por momentum
- Animações básicas de movimento
- VFX para pulo, segundo pulo e dash
- Primeiro cenário de testes

---

> Este README é atualizado conforme novas mecânicas e sistemas relevantes são adicionados ao projeto.
