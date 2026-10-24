namespace VoisinApp.Api.Services;

public interface IServiceEmail
{
    Task EnvoyerAsync(string destinataire, string sujet, string corps);
}

// Version de développement : affiche l'e-mail dans la console de l'API au lieu de l'envoyer.
// Pour envoyer de vrais e-mails, créez une autre classe qui implémente IServiceEmail
// (SMTP, SendGrid, Brevo...) et remplacez-la dans Program.cs.
public class ServiceEmailConsole(ILogger<ServiceEmailConsole> journal) : IServiceEmail
{
    public Task EnvoyerAsync(string destinataire, string sujet, string corps)
    {
        journal.LogInformation("E-MAIL pour {Destinataire} | {Sujet}\n{Corps}", destinataire, sujet, corps);
        return Task.CompletedTask;
    }
}
