# Metal Sonic — Tempus Gemini (Unity 2D)

Clone 2D de Sonic em Unity, em desenvolvimento. Personagem: Metal Sonic.
Documento de design: `GDD_10_Folhas (Novo).pdf`.

## Requisitos

- Unity **6000.5.10f1** (ver `ProjectSettings/ProjectVersion.txt`)
- Pipeline URP 2D (assets em `Assets/Settings/`)

## Setup

1. Abra o projeto no Hub com a versao acima.
2. Abra `Assets/Scenes/TestLevel.unity` (cena de testes) ou `Assets/Scenes/SampleScene.unity`.
3. Play.

## Estrutura

    Assets/
      Editor/       scripts de editor (geracao de assets, montagem de fase, testes)
      Scenes/       SampleScene, TestLevel
      Scripts/      HeightfieldBridge, MetalSonicAnimator, MetalSonicController,
                    MetalSonicInput, MetalSonicMechanics, MetalSonicPhysics,
                    MetalSonicSpriteBank, MetalSonicWorld, SonicMovement
      Sprites/      sprites soltos (referencia)
      Resources/Sprites/   mesmos sprites carregados via Resources.Load
      Settings/     URP 2D + Input System

### Por que os sprites estao duplicados?

`Assets/Sprites/` e `Assets/Resources/Sprites/` contem os **mesmos 61 PNGs** (verificados
byte a byte). Nao foi intencional: os arquivos foram parar nas duas pastas durante a
montagem do projeto. `Resources/Sprites/` e o que o codigo carrega em runtime; `Assets/Sprites/`
e so referencia. Para remover a duplicacao, apague `Assets/Sprites/` **junto com a sua pasta
`.meta`** (Unity exige os dois juntos) depois de confirmar que nada o referencia.

## Controles

Definidos em `Assets/Settings/InputSystem_Actions.inputactions`. O projeto usa
`activeInputHandler` do novo Input System: input legado (`Input.GetKey`) nao funciona.
Se os botoes de UI pararem de responder, confira esse setting em Project Settings > Player.

## Git

`.gitignore` segue o template oficial do Unity (ignora `Library/`, `Temp/`, `Logs/`,
`UserSettings/`, builds e meta de `.sln`/`.csproj`). Nada do projeto e ignorado de forma
inesperada: `Library/` (~2,2 GB) fica de fora do repo, e tudo em `Assets/` e
`ProjectSettings/` entra normalmente.

## Notas de desenvolvimento

- `Assets/Scenes/TestLevel.unity` foi gerada por `Assets/Editor/MetalSonicLevelBuilder.cs`.
- Sem audio ainda: `ProjectSettings/AudioManager.asset` esta no padrao do template.
