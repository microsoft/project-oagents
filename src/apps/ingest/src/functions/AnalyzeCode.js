const { app } = require("@azure/functions");

const Parser = require("tree-sitter");
const CSharp = require("tree-sitter-c-sharp");
const {Query, QueryCursor} = Parser
const parser = new Parser();
parser.setLanguage(CSharp);



app.http("AnalyzeCode", {
  methods: ["POST"],
  authLevel: "anonymous",
  handler: async (request, context) => {
    context.log(`Http function processed request for url "${request.url}"`);
    const sourceCode = request.body.json();
    const tree = parser.parse(sourceCode.Content);
    const query = new Query(CSharp, `((comment) @method-comment
                                      (method_declaration) @method-declaration )`);
    
    const matches = query.matches(tree.rootNode);
    var items = [];
    for (let match of matches) {
      const captures = match.captures;
      var item = {};
      for (let capture of captures) {
        if (capture.name === 'method-comment') {
            item.comment = tree.getText(capture.node);
        }
        else if (capture.name === 'method-declaration') {
            item.code = tree.getText(capture.node);
        }
      }
        items.push(item);
    }
    
    return { jsonBody: items};
  },
});
