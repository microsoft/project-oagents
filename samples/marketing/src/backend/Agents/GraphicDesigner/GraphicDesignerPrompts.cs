
namespace Marketing.Agents;
public static class GraphicDesignerPrompts
{
    public static string GenerateImage = """
        You are a Marketing community manager graphic designer. 
        Bellow is a campaing that you need to create a image for.
        Create an image of maximum 500x500 pixels that could be use in social medias as a marketing iamge.
        Only answer if the user is asking for an image to be created or if the user is asking for a new compaing
        Input: {{$input}}
        """;
}