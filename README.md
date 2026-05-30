# Conkist GDK Services

O **Conkist GDK Services** é um framework extensível e unificado para integração e gerenciamento de serviços de backend (como autenticação, salvamento em nuvem, analytics e dados remotos) em jogos na Unity.

Em vez de ser apenas um kit de integração direta para um serviço específico, o pacote adota uma **arquitetura em duas camadas**, permitindo desacoplar a lógica do seu jogo dos provedores de serviço (providers).

---

## Arquitetura em Duas Camadas

### 1. Camada de Padronização (Standardization Layer)
Esta camada define interfaces C# abstratas e contratos unificados para operações comuns de backend. O código do seu jogo interage apenas com estas abstrações, sem saber qual backend real está sendo utilizado por baixo.

*   `IAuthService`: Contratos para Login anônimo, login por usuário/senha, vinculação de contas e logout.
*   `IRemoteDataService`: Contratos para salvar, ler e sincronizar dados de perfil e progresso do jogador em nuvem.
*   `IAnalyticsService`: Contratos para rastrear eventos customizados, progresso e telemetria de jogo.

### 2. Camada de Provedores (Provider Integration Layer)
Esta camada contém as implementações concretas das interfaces de padronização para diferentes backends do mercado.

*   **Conkist Backend Service**: Implementação dedicada integrada aos servidores e serviços proprietários da Conkist.
*   **Unity Gaming Services (UGS)**: Implementação concreta utilizando serviços oficiais da Unity (Authentication, Cloud Save, Analytics).
*   **PlayFab**: Implementação concreta para a plataforma de backend PlayFab da Microsoft.

---

## Fachada Unificada (Unified Facade)

O framework expõe um ponto de acesso global configurável. Você pode chavear entre o provedor da Unity (UGS), o provedor do PlayFab ou o provedor do serviço Conkist sem alterar uma única linha de código nas classes que consomem os serviços.

### Exemplo de Configuração e Uso

```csharp
using Conkist.GDK.Services;
using UnityEngine;

public class ServiceBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Inicializar a Fachada com o Provedor de Serviços da Conkist
        IAuthService authProvider = new ConkistAuthService();
        IRemoteDataService dataProvider = new ConkistRemoteDataService();

        // Registrar os provedores no gerenciador global
        GDKServices.RegisterAuth(authProvider);
        GDKServices.RegisterRemoteData(dataProvider);
    }
}
```

### Exemplo de Consumo no Jogo

```csharp
using Conkist.GDK.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoginController : MonoBehaviour
{
    public async UniTask Void RealizarLogin()
    {
        // O código do jogo consome a interface unificada de forma transparente
        bool sucesso = await GDKServices.Auth.LoginAnônimoAsync();

        if (sucesso)
        {
            Debug.Log("Login efetuado com sucesso.");
            // Carregar progresso do jogador
            var dados = await GDKServices.RemoteData.LoadDataAsync("ProgressoJogador");
        }
    }
}
```

---

## Instalação e Dependências

Este pacote requer a presença do **Conkist GDK Core** e do **UniTask** no projeto.

Adicione as referências no arquivo `Packages/manifest.json` do seu projeto Unity:

```json
"dependencies": {
  "me.conkist.gdk.core": "https://github.com/Conkist/Conkist.GDK.Core.git",
  "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11",
  "me.conkist.gdk.services": "https://github.com/Conkist/Conkist.GDK.Services.git"
}
```

---

## Amostras (Samples)

O pacote acompanha exemplos demonstrativos na pasta `Samples~/` mostrando:
1.  Como estruturar a inicialização (`Bootstrap`).
2.  Como alternar entre a integração com a **Unity** e com a **Conkist** a partir do mesmo objeto fachada.