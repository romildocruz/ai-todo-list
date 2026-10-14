# MongoDB local no Windows (sem Docker)

Guia para verificar, iniciar e configurar o MongoDB usado pela API. Nenhuma etapa exige Docker, Compose, contêineres ou Kubernetes.

Valores do projeto:

| Item | Valor |
|---|---|
| Connection string (dev) | `mongodb://localhost:27017` |
| Banco | `AiTodoDb` |
| Coleção | `Todos` |
| Banco de testes de integração | `AiTodoDb_Tests_<guid>` (criado e descartado pelos testes) |

## 1. Verificar a instalação

Execute no terminal do VS Code (PowerShell):

```powershell
# O serviço existe? (nome padrão do instalador MSI: MongoDB)
Get-Service *mongo*

# Detalhes: caminho do executável e do arquivo de configuração
Get-CimInstance Win32_Service -Filter "Name='MongoDB'" | Select-Object State, StartMode, PathName | Format-List

# Os executáveis estão no PATH? (podem não estar mesmo com o serviço instalado)
Get-Command mongod, mongosh -ErrorAction SilentlyContinue
```

Observações:

- O `PathName` do serviço mostra onde o `mongod.exe` está instalado (ex.: `D:\MongoDB\Server\9.0\bin\mongod.exe`) e qual `mongod.cfg` ele usa. **A API não precisa de `mongod` nem de `mongosh` no PATH**, apenas do serviço em execução.
- Para consultar a versão sem alterar o PATH: `& "D:\MongoDB\Server\9.0\bin\mongod.exe" --version` (ajuste o caminho).

## 2. Iniciar e verificar o serviço

Iniciar e parar exigem PowerShell **como administrador**:

```powershell
Get-Service MongoDB            # Status: Running / Stopped
Start-Service MongoDB
Stop-Service MongoDB
Restart-Service MongoDB
```

Para iniciar o serviço automaticamente com o Windows: `Set-Service MongoDB -StartupType Automatic`.

## 3. Verificar a porta

A porta padrão é `27017`. Confirme o valor no `mongod.cfg` (seção `net`), onde também se vê `bindIp` (`127.0.0.1` = somente esta máquina):

```powershell
Test-NetConnection localhost -Port 27017          # TcpTestSucceeded : True
Select-String -Path "D:\MongoDB\Server\9.0\bin\mongod.cfg" -Pattern "port|bindIp|authorization|dbPath"
```

Se `TcpTestSucceeded` for `False`, o serviço está parado ou usa outra porta: ajuste a connection string.

## 4. Confirmar a conexão

Com `mongosh` instalado:

```powershell
mongosh "mongodb://localhost:27017" --eval "db.runCommand({ ping: 1 })"   # { ok: 1 }
```

Sem `mongosh`, use o próprio projeto: ao subir a API, o log deve mostrar
`Índices garantidos em AiTodoDb.Todos: ix_createdAt_desc, ix_status_createdAt_desc`.
Se o MongoDB estiver indisponível, a API sobe normalmente e registra
`MongoDB indisponível (TimeoutException); índices não criados. Nova tentativa em 10s.`

## 5. Se o MongoDB não estiver instalado

1. Baixe o **MongoDB Community Server** (pacote **MSI**) em <https://www.mongodb.com/try/download/community>.
2. No instalador, escolha **Complete** e mantenha marcado **Install MongoDB as a Service** (serviço `MongoDB`, execução como *Network Service*, início automático).
3. Opcional: marque **MongoDB Compass** (interface gráfica). O `mongosh` (shell) é um instalador à parte em <https://www.mongodb.com/try/download/shell>.
4. Repita as verificações das seções 1 a 3.

## 6. Configurar a connection string

A API lê a seção `Mongo` da configuração. A connection string **não** fica em `appsettings.json` (que é versionado). Escolha uma das opções (a última vence):

**a) Arquivo local (recomendado para começar).** Copie o exemplo; o arquivo local é ignorado pelo Git:

```powershell
cd src\AiTodo.Api
Copy-Item appsettings.Development.json.example appsettings.Development.json
```

**b) User-secrets** (bom para credenciais, ficam fora do repositório):

```powershell
dotnet user-secrets set "Mongo:ConnectionString" "mongodb://localhost:27017" --project src/AiTodo.Api
```

**c) Variável de ambiente** (prevalece sobre os arquivos):

```powershell
$env:Mongo__ConnectionString = "mongodb://localhost:27017"
```

Se nenhuma estiver definida, a API **não inicia** e informa: `A chave 'Mongo:ConnectionString' não está configurada...`.

Demais chaves (em `appsettings.json`): `Mongo:DatabaseName` (`AiTodoDb`), `Mongo:CollectionName` (`Todos`), `Mongo:ServerSelectionTimeoutSeconds` e `Mongo:ConnectTimeoutSeconds` (3 s).

### Servidor com autenticação

Se o `mongod.cfg` tiver `security: authorization: enabled`, informe usuário e senha **somente** via user-secrets ou variável de ambiente, nunca em arquivos versionados:

```powershell
dotnet user-secrets set "Mongo:ConnectionString" "mongodb://USUARIO:SENHA@localhost:27017/?authSource=admin" --project src/AiTodo.Api
```

Caracteres especiais na senha devem ser codificados em URL (ex.: `@` vira `%40`). O usuário precisa de permissão de leitura e escrita em `AiTodoDb`.

## 7. Banco, coleção e dados

- O banco `AiTodoDb` e a coleção `Todos` são criados automaticamente na primeira escrita; a API também cria os índices ao iniciar (criação idempotente).
- Para inspecionar os dados, abra o **MongoDB Compass**, conecte em `mongodb://localhost:27017` e navegue até `AiTodoDb` > `Todos` (aba *Indexes* mostra `ix_createdAt_desc` e `ix_status_createdAt_desc`).

## 8. Problemas comuns

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| `TcpTestSucceeded : False` | Serviço parado ou porta diferente | `Start-Service MongoDB`; conferir `port` no `mongod.cfg` |
| Log `MongoDB indisponível (TimeoutException)` | Serviço parado ou connection string errada | Verificar seções 2, 3 e 6; a API tenta de novo a cada 10 s |
| `Start-Service` negado | PowerShell sem privilégio | Abrir como administrador |
| Erro de autenticação (`Unauthorized`) | Servidor exige credenciais | Usar a seção "Servidor com autenticação" |
| API não inicia citando `Mongo:ConnectionString` | Configuração ausente | Seção 6 |
