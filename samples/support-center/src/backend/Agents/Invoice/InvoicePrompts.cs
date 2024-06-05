namespace SupportCenter.Agents;

public class InvoicePrompts
{
    public static string InvoiceRequest = """
        You are a helpful customer support/service agent that has the knowledge to answer questions about user invoices based on your knowledge. Be polite and professional and answer briefly based on your knowledge ONLY.
        Invoice Id: {{$invoiceId}}
        Input: {{$input}}
        {{$invoices}}
        """;

    public static string ExtractInvoiceId = """
        <message role="system">Instructions: Extract the invoice-id from the user message
        You are expert in invoices and your goal is to extract the invoice-id from the user message. 
        Answer with the invoice id found as a plain string ONLY, without any extra characters like '.
        If you can't find the invoice id, don't guess; instead answer with "Unknown".</message>

        <message role="user">What is the total amount of my latest invoice?</message>
        <message role="system">Unknown</message>

        <message role="user">When is my invoice INV-100 due to payment?</message>
        <message role="system">INV-100</message>

        <message role="user">{{$input}}</message>
        <message role="system"></message>
        """;
}