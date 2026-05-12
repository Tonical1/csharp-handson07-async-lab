# ⚡ AsyncLab

Enrico Ricarte - RM558571  
Pedro Gaspar - RM554887  
Victor Freire - RM556191  

## 🧪 Laboratório Async

### 🎯 Objetivo
Analisar o programa e tornar a sua execução **assíncrona**.

### 📝 Atividades
- 🔍 Identificar pontos do programa que podem ser transformados em chamadas assíncronas;  
- ⏱️ Observar o impacto no tempo de execução;  

---

## 🛠️ Modificações Realizadas

Para tornar o programa mais eficiente, as seguintes alterações foram realizadas:

1. **Uso de `Task` e paralelismo controlado**:
   - Substituímos loops sequenciais por tarefas assíncronas (`Task`) para processar os municípios em paralelo.
   - Utilizamos `ConcurrentDictionary` e `ConcurrentBag` para garantir segurança em operações concorrentes.

2. **Fila de execução paralela (`QueueCreator`)**:
   - Implementamos uma fila para limitar o número de tarefas simultâneas, evitando sobrecarga do sistema.

3. **Progresso dinâmico com `Spectre.Console`**:
   - Adicionamos uma interface de progresso que atualiza em tempo real, permitindo monitorar o andamento do processamento.

4. **Aproveitamento de múltiplos núcleos**:
   - O número de tarefas simultâneas foi ajustado com base em `Environment.ProcessorCount`, garantindo uso eficiente do hardware.

---

## 📊 Impactos Observados no Tempo de Execução

Após as modificações, os seguintes impactos foram observados:

- **Tempo de leitura do arquivo**:
  - A leitura do arquivo CSV foi otimizada e agora leva em média **2.5s a 3s**.

- **Tempo total de execução**:
  - O tempo total inclui o tempo de download do arquivo pela API e o tempo de leitura e processamento.  
  - **Exemplo de tempos observados**:
    - **Download do arquivo**: ~1s (dependendo da conexão).
    - **Leitura e processamento**: ~2.5s a 3s.
    - **Tempo total**: ~3.5s a 4s.

- **Melhor uso de recursos do sistema**:
  - A aplicação agora utiliza múltiplos núcleos do processador, reduzindo o tempo ocioso.

---

## 🌐 Repositório
[https://github.com/profvinicius84/AsyncLab](https://github.com/profvinicius84/AsyncLab)

---

👨‍🏫 © 2025 | Professor Vinícius Costa Santos
