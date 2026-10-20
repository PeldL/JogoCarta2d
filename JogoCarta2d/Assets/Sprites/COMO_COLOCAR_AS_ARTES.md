# Colocar as artes

1. Coloque as imagens nas pastas `Sprites/Personagens` ou `Sprites/Cenarios`, dentro da pasta com o nome correspondente.
2. Selecione cada imagem na Unity, configure **Texture Type → Sprite (2D and UI)** e pressione **Apply**. Para uma imagem inteira, use **Sprite Mode → Single**.
3. Selecione **Resources/ArtesCasoZero**. No Inspector, arraste cada sprite para o campo **Sprite** do nome correspondente. Não altere os nomes da biblioteca: eles ligam os espaços às imagens.
4. Entre no Play. Os cenários Bar, Delegacia, Hospital, Escritório e Apartamento já consultam essa biblioteca. Quando recebem a arte, os desenhos provisórios decorativos daquele cenário somem; os controles da investigação continuam funcionando. Sem sprite, o rascunho continua aparecendo.

## Personagens e locais futuros

Em **Prefabs/Artes**, há um prefab para cada um dos 11 personagens e cada um dos 9 cenários. Arraste o prefab para dentro de um **Canvas**, ajuste a posição e o tamanho pelo Rect Transform e dê Play. Ele usa a imagem do nome correspondente na biblioteca. Os personagens mantêm a proporção da imagem.

O componente **Case Zero Art Slot** também tem **Sprite Override** para usar uma imagem diferente somente naquele objeto. Não é necessário alterar os scripts.

Os personagens ainda não foram associados a interrogatórios, culpados ou uma ordem de aparição: a equipe vai definir esse fluxo. A vítima 1 é a mulher do bar. A biblioteca separa o barista dos outros personagens e não assume que ele seja o culpado.

Laboratório de perícia, Padaria, Rua e Estacionamento têm pastas, campos e prefabs preparados; não foram criadas novas rotas ou cenas jogáveis para esses locais.

Os cenários usam um quadro de referência de **1920 × 1080** e preenchem esse quadro. Para evitar distorção, exporte a arte de cenário na proporção **16:9**. Os prefabs são controles de UI; precisam de um Canvas para aparecer.

As pastas organizam os arquivos; colocar uma imagem na pasta sozinho não preenche a biblioteca. É necessário arrastá-la para o campo uma vez. A atualização pelo catálogo acontece durante o Play. Nenhuma arte final foi gerada.

## Interrogatório e dedução

Na biblioteca **ArtesCasoZero**, abra **Retratos**. Cada entrada mostra o personagem provisório do caso. Arraste a imagem para **Sprite**, ou preencha **Art Name** com um nome exato da lista **Personagens** para reutilizar aquela arte. Sem imagem, o espaço fica branco com o nome embaixo; não é necessário gerar sprites com texto.

O interrogatório exibe o retrato da pessoa selecionada. A dedução mostra os retratos nos botões existentes, com rolagem vertical pela roda do mouse ou arrastando a lista. Os objetos estão salvos na cena **UI**, para editar o tamanho e a posição. A escolha de imagem não altera a identidade nem as regras do suspeito.

## Retratos do rádio e celular

Em **ArtesCasoZero**, preencha **Radio Detetive** e **Radio Central** com os sprites dos dois participantes. O detetive aparece à esquerda e a central à direita, com a frequência no meio e a fala embaixo. Sem arte, ficam espaços brancos identificados pelo nome.

No celular, **Enviar para análise** fica disponível após reunir as quatro provas do cenário. A perícia retorna por ligação; atenda e registre o laudo para liberar a conclusão junto com os depoimentos. É possível continuar investigando durante a análise e salvar/carregar sem perder o pedido. **Documentos**, **Pedir orientação** e **Consultar laudo** permitem consultar o material do caso.

## Cursores opcionais

Na mesma biblioteca há três campos opcionais: **Cursor Lupa**, **Cursor Interacao** e **Cursor Escrita**. Importe essas imagens com **Texture Type → Cursor**, aplique e arraste a textura para o campo. Use os campos **Hotspot** para indicar o ponto de clique em pixels a partir do canto superior esquerdo.

- Lupa: áreas sem controle interativo.
- Interação: botões e outros controles habilitados, incluindo o slider do rádio.
- Escrita: sobre um campo de texto editável.

Um campo de cursor vazio mantém o cursor padrão nesse estado. Não foram criadas artes ou animações de cursor. Os cursores são independentes dos sprites de personagem e não mudam os cliques do jogo.
