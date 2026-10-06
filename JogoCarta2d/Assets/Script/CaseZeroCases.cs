// Case content is separate from scene objects. Evidence keys are local to a case;
// the five legacy keys preserve the existing scene button bindings and old saves.
public sealed class CaseZeroCase
{
    public string id, title, location, introduction, call, ending;
    public string[] evidenceNames, evidenceDescriptions, suspects, statements, explanations;
    public string[] objects, places;
    public string objectQuestion, placeQuestion;
    public int culprit;
}

public static class CaseZeroCases
{
    public static readonly string[] EvidenceKeys = { "registro", "pistola", "capsula", "sangue", "depoimento" };
    public static readonly CaseZeroCase[] All = {
        new CaseZeroCase {
            id = "ultima-testemunha", title = "A Última Testemunha", location = "Bar", culprit = 0,
            introduction = "Uma mulher entrou no bar e foi morta a tiros. Reúna as provas, confronte o homem do balcão e descubra culpado, arma e local.",
            suspects = new[] { "Homem do balcão", "Funcionário do bar", "Cliente desconhecido" },
            objects = new[] { "Pistola", "Faca", "Veneno" }, places = new[] { "Bar", "Rua", "Casa da vítima" },
            objectQuestion = "COM QUAL ARMA?", placeQuestion = "ONDE?",
            ending = "O homem do balcão era um assaltante. Ele matou a mulher com a pistola no bar para impedir que ela o denunciasse.\n\nNOVA OCORRÊNCIA: Helena Duarte foi encontrada desacordada no Apartamento 302. Um pendrive desapareceu."
        },
        new CaseZeroCase {
            id = "apartamento-302", title = "O Apartamento 302", location = "Apartamento", culprit = 2,
            introduction = "Helena Duarte está hospitalizada após um ataque. Seu pendrive desapareceu. Descubra quem roubou o dispositivo; o responsável pelo ataque ainda será investigado.",
            evidenceNames = new[] { "Registro do elevador", "Marca de sapato", "Mensagens e visitas", "Registro da portaria", "Depoimento confrontado" },
            evidenceDescriptions = new[] {
                "Às 22h07, Carlos usou sua chave de serviço para acessar o terceiro andar. A câmera mostra que ele saiu às 22h12 carregando um pequeno dispositivo. Ele negou ter subido.",
                "A marca junto à mesa do notebook corresponde ao calçado de trabalho de Carlos. Uma foto da vistoria do prédio permite comparar a sola. A marca confirma sua presença, mas não prova o ataque.",
                "Helena escreveu às 21h40: 'Marina já saiu; guardei as provas da Vértice no pendrive'. A câmera confirma a saída de Marina às 21h30. O notebook permaneceu no apartamento, mas o pendrive sumiu.",
                "André encerrou a manutenção às 16h e devolveu o acesso. Lucas esteve com o porteiro das 22h às 22h20: a discussão anterior era sobre barulho. Às 22h15, o porteiro encontrou Helena caída. Carlos foi o único dos quatro a entrar no apartamento nesse intervalo.",
                "Carlos admite que usou a chave reserva e levou o pendrive. Helena já estava caída quando entrou. Em seu celular: 'Pegue o dispositivo. Nada além disso'. O contato era desconhecido."
            },
            suspects = new[] { "Marina • amiga", "Lucas • vizinho", "Carlos • síndico", "André • técnico" },
            statements = new[] {
                "Visitei Helena, mas saí às 21h30. Ela estava preocupada com documentos do trabalho. Não peguei nada.",
                "Discutimos por causa do som à tarde. À noite fiquei conversando com o porteiro. Não entrei no apartamento.",
                "Tenho uma chave reserva, mas não subi ao terceiro andar naquela noite. Estava cuidando do prédio.",
                "Consertei o notebook durante a tarde. Terminei às 16h e fui embora. O pendrive estava lá quando saí."
            },
            explanations = new[] {
                "A câmera e a mensagem de Helena confirmam: saí antes do desaparecimento do pendrive. Ela continuou com o dispositivo após minha visita.",
                "O registro da portaria confirma meu horário. A discussão sobre barulho não teve relação com o roubo.",
                "Está bem. O elevador e a pegada são meus. Peguei o pendrive por dinheiro, mas Helena já estava caída. A ordem dizia: 'Pegue o dispositivo. Nada além disso'.",
                "A ordem de serviço confirma que saí às 16h. O registro de Helena mostra que o pendrive ainda estava com ela à noite."
            },
            objects = new[] { "Pendrive", "Notebook", "Dinheiro" }, places = new[] { "Apartamento 302", "Portaria", "Estacionamento" },
            objectQuestion = "O QUE FOI ROUBADO?", placeQuestion = "ONDE OCORREU O ROUBO?",
            call = "Helena acordou. Ela confirma que guardava provas da empresa em um pendrive e pede: 'Procure por Vértice'. Identifique o autor do roubo; não confunda o roubo com o ataque.",
            ending = "Carlos roubou o pendrive no Apartamento 302. As provas confirmam o roubo, não a autoria do ataque.\n\nHelena acordou: o dispositivo continha provas contra a Vértice. A próxima investigação será no escritório da empresa."
        },
        new CaseZeroCase {
            id = "arquivo-vertice", title = "Arquivo Vértice", location = "Escritorio", culprit = 1,
            introduction = "Helena descobriu pagamentos suspeitos. Examine o escritório, ouça os quatro envolvidos e identifique quem comandava o esquema, qual era o crime e qual empresa servia de fachada.",
            evidenceNames = new[] { "Planilha de transferências", "Cadastro da Vértice", "Registros de acesso", "Ordens e relatório", "Depoimento confrontado" },
            evidenceDescriptions = new[] {
                "Pagamentos sem serviço correspondente foram autorizados com a assinatura digital de Augusto Mendes. Beatriz anotou as irregularidades e enviou uma denúncia antes de Helena ser atacada.",
                "Vértice Serviços não tem funcionários nem entregas registradas. Sua conta é controlada por Augusto. Horizonte e Atlas, citadas na planilha, possuem contratos e serviços comprovados.",
                "A exclusão de arquivos usou a conta de Fábio, mas partiu do computador de Augusto quando Fábio estava fora da empresa. O relatório de suporte confirma a ausência do técnico.",
                "Mensagens autenticadas de Augusto ordenam recuperar o pendrive e intimidar Helena. A ordem a Carlos restringia-se ao roubo. Um relatório identifica um executor externo como autor do ataque, por ordem de Augusto. Renato estava no posto de segurança; suas imagens e registros ajudam a confirmar os horários.",
                "Confrontado com a assinatura digital, a conta da Vértice e as mensagens, Augusto admite que comandava os desvios e a operação contra Helena."
            },
            suspects = new[] { "Beatriz • contadora", "Augusto • diretor", "Renato • segurança", "Fábio • técnico" },
            statements = new[] {
                "Preparei as planilhas, mas não autorizei aqueles pagamentos. Avisei que não havia serviço entregue.",
                "A Vértice era apenas uma fornecedora. Não controlo a conta dela e não sei quem apagou os arquivos.",
                "Fui visto perto de Helena durante minha ronda. Meu turno e as câmeras mostram onde eu estava.",
                "Usaram minha conta para apagar arquivos. Eu estava atendendo outra empresa naquele horário."
            },
            explanations = new[] {
                "Minha denúncia foi enviada antes do ataque. A assinatura que liberava as transferências é de Augusto, não minha.",
                "Os registros são verdadeiros. Eu controlava a Vértice e autorizava os desvios. Também mandei recuperar as provas e intimidar Helena. Carlos só deveria levar o pendrive.",
                "Entreguei os registros da ronda. Eles confirmam meu turno e a movimentação do diretor; eu não comandava o esquema.",
                "O relatório de suporte confirma minha ausência. O acesso saiu do computador do diretor. Minha conta foi usada sem minha autorização."
            },
            objects = new[] { "Desvio de dinheiro", "Roubo de equipamentos", "Falsificação de produtos" },
            places = new[] { "Vértice Serviços", "Horizonte", "Atlas" },
            objectQuestion = "QUAL ERA O CRIME?", placeQuestion = "QUAL EMPRESA DE FACHADA?",
            call = "A análise confirmou as assinaturas digitais e a origem das mensagens. A conta da Vértice é controlada pelo diretor. O relatório também liga a ordem de intimidação ao ataque contra Helena. Cruze essas provas com os depoimentos.",
            ending = "Augusto comandava o desvio de dinheiro usando a Vértice Serviços. Os documentos também ligam suas ordens ao ataque contra Helena.\n\nCarlos responde pelo roubo; o executor identificado no relatório, pelo ataque. Helena entrega a cópia das provas. Os três casos estão concluídos."
        }
    };
    public static int Index(string id) => System.Array.FindIndex(All, c => c.id == id);
    public static CaseZeroCase Get(string id) => All[System.Math.Max(0, Index(id))];
}
