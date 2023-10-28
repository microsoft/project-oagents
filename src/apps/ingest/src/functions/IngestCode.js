const { app } = require("@azure/functions");

const Parser = require("tree-sitter");
const CSharp = require("tree-sitter-c-sharp");
const {Query, QueryCursor} = Parser
const parser = new Parser();
parser.setLanguage(CSharp);
const sourceCode = `
namespace Foo
{
  // a class modeling a Dog
  public class Dog {
      private string name;
      // method that gets the name of the Dog
      public string GetName(){
          return name;
      }
      // a static method getting the current date
      public static string GetDate()
      {
          return DateTime.Now.ToString("MM/dd/yyyy");
      }
  }
}`;
const tree = parser.parse(sourceCode);

app.http("IngestCode", {
  methods: ["GET", "POST"],
  authLevel: "anonymous",
  handler: async (request, context) => {
    context.log(`Http function processed request for url "${request.url}"`);
    //context.log(tree.rootNode.toString());


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
    var result = JSON.stringify(items);
    return { body: result};
  },
});
