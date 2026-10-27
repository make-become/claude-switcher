// Every string the user sees, in every language the app speaks: the Mac app's table, with the
// Windows words where the two differ (taskbar for Dock, notification area for menu bar).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClaudeSwitcher {

static class L10n {
    static readonly string[] languages = { "en", "it", "es", "fr", "de" };

    /// CLAUDE_SWITCHER_LANG, else Windows' display language when we have it, else English.
    public static readonly string Lang = Pick();
    static string Pick() {
        string forced = Environment.GetEnvironmentVariable("CLAUDE_SWITCHER_LANG");
        if (forced != null && languages.Contains(forced)) return forced;
        string ui = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return languages.Contains(ui) ? ui : "en";
    }

    public static string T(string key, params object[] args) {
        Dictionary<string, string> row;
        string s = null;
        if (strings.TryGetValue(key, out row) && !row.TryGetValue(Lang, out s)) row.TryGetValue("en", out s);
        if (s == null) s = key;
        return args.Length == 0 ? s : string.Format(s, args);
    }

    static Dictionary<string, string> R(string en, string it, string es, string fr, string de) {
        return new Dictionary<string, string> { { "en", en }, { "it", it }, { "es", es }, { "fr", fr }, { "de", de } };
    }

    static readonly Dictionary<string, Dictionary<string, string>> strings = new Dictionary<string, Dictionary<string, string>> {
        { "open", R("Open", "Apri", "Abrir", "Ouvrir", "Öffnen") },
        { "front", R("Bring to front", "Porta davanti", "Traer al frente", "Mettre au premier plan", "In den Vordergrund") },
        { "quitProfile", R("Quit {0}", "Esci da {0}", "Salir de {0}", "Quitter {0}", "{0} beenden") },
        { "safeLogin", R("Safe login", "Login sicuro", "Inicio de sesión seguro", "Connexion sûre", "Sicher anmelden") },
        { "openAtLogin", R("Open when Windows starts", "Apri all'avvio di Windows", "Abrir al iniciar Windows",
                           "Ouvrir au démarrage de Windows", "Beim Start von Windows öffnen") },
        { "ownIcon", R("Own taskbar icon", "Icona propria nella barra delle applicazioni", "Icono propio en la barra de tareas",
                       "Sa propre icône dans la barre des tâches", "Eigenes Symbol in der Taskleiste") },
        { "edit", R("Edit…", "Modifica…", "Editar…", "Modifier…", "Bearbeiten…") },
        { "delete", R("Delete profile…", "Elimina profilo…", "Eliminar perfil…", "Supprimer le profil…", "Profil löschen…") },
        { "manage", R("Manage", "Gestisci", "Gestionar", "Gérer", "Verwalten") },
        { "newProfile", R("New profile…", "Nuovo profilo…", "Nuevo perfil…", "Nouveau profil…", "Neues Profil…") },
        { "showTray", R("Show in the notification area", "Mostra nell'area di notifica", "Mostrar en el área de notificación",
                        "Afficher dans la zone de notification", "Im Infobereich zeigen") },
        { "switcherAtLogin", R("Start Claude Switcher at login", "Avvia Claude Switcher al login", "Iniciar Claude Switcher al iniciar sesión",
                               "Lancer Claude Switcher à l'ouverture de session", "Claude Switcher bei der Anmeldung starten") },
        { "quitSwitcher", R("Quit Claude Switcher", "Esci da Claude Switcher", "Salir de Claude Switcher",
                            "Quitter Claude Switcher", "Claude Switcher beenden") },
        { "sameAccount", R("⚠ same account as {0}", "⚠ stesso account di {0}", "⚠ misma cuenta que {0}",
                           "⚠ même compte que {0}", "⚠ gleiches Konto wie {0}") },
        { "loginTitle", R("Log in to {0}", "Login {0}", "Iniciar sesión en {0}", "Connexion à {0}", "Bei {0} anmelden") },
        { "loginBody", R("For the next 10 minutes, a sign-in that comes back from the browser goes to {0}, whichever profiles are open.",
                         "Per i prossimi 10 minuti, un login che torna dal browser va a {0}, qualunque profilo sia aperto.",
                         "Durante los próximos 10 minutos, un inicio de sesión que vuelve del navegador va a {0}, estén abiertos los perfiles que estén.",
                         "Pendant les 10 prochaines minutes, une connexion qui revient du navigateur va à {0}, quels que soient les profils ouverts.",
                         "In den nächsten 10 Minuten geht eine Anmeldung, die aus dem Browser zurückkommt, an {0}, egal welche Profile offen sind.") },
        { "loginDone", R("Signed in: {0}", "Login fatto: {0}", "Sesión iniciada: {0}", "Connecté : {0}", "Angemeldet: {0}") },
        { "cancel", R("Cancel", "Annulla", "Cancelar", "Annuler", "Abbrechen") },
        { "deleteTitle", R("Delete the profile {0}?", "Eliminare il profilo {0}?", "¿Eliminar el perfil {0}?",
                           "Supprimer le profil {0} ?", "Profil {0} löschen?") },
        { "deleteBody", R("Its icon leaves the taskbar, the Start menu and the menu. Nothing else is deleted: the account data stays in {0}.",
                          "L'icona sparisce dalla barra delle applicazioni, dal menu Start e dal menu. Nient'altro viene cancellato: i dati dell'account restano in {0}.",
                          "Su icono sale de la barra de tareas, del menú Inicio y del menú. No se borra nada más: los datos de la cuenta quedan en {0}.",
                          "Son icône quitte la barre des tâches, le menu Démarrer et le menu. Rien d'autre n'est effacé : les données du compte restent dans {0}.",
                          "Das Symbol verschwindet aus Taskleiste, Startmenü und Menü. Sonst wird nichts gelöscht: die Kontodaten bleiben in {0}.") },
        { "defaultData", R("Claude's own folder (default profile)", "la cartella di Claude (profilo predefinito)",
                           "la carpeta de Claude (perfil predeterminado)", "le dossier de Claude (profil par défaut)",
                           "Claudes eigenem Ordner (Standardprofil)") },
        { "deleteButton", R("Delete", "Elimina", "Eliminar", "Supprimer", "Löschen") },
        { "newTitle", R("New profile", "Nuovo profilo", "Nuevo perfil", "Nouveau profil", "Neues Profil") },
        { "editTitle", R("Edit {0}", "Modifica {0}", "Editar {0}", "Modifier {0}", "{0} bearbeiten") },
        { "save", R("Save", "Salva", "Guardar", "Enregistrer", "Sichern") },
        { "name", R("Name", "Nome", "Nombre", "Nom", "Name") },
        { "letter", R("Letter", "Lettera", "Letra", "Lettre", "Buchstabe") },
        { "color", R("Color", "Colore", "Color", "Couleur", "Farbe") },
        { "email", R("E-mail", "E-mail", "Correo", "E-mail", "E-Mail") },
        { "namePh", R("Name (e.g. Work)", "Nome (es. Work)", "Nombre (p. ej. Trabajo)", "Nom (ex. Travail)", "Name (z. B. Arbeit)") },
        { "emailPh", R("Account e-mail (just a reminder)", "E-mail dell'account (solo promemoria)", "Correo de la cuenta (solo recordatorio)",
                       "E-mail du compte (simple rappel)", "Konto-E-Mail (nur zur Erinnerung)") },
        { "needName", R("A name is required.", "Serve un nome.", "Hace falta un nombre.", "Il faut un nom.", "Ein Name ist nötig.") },
        { "nameTaken", R("A profile named {0} already exists.", "Esiste già un profilo {0}.", "Ya existe un perfil {0}.",
                         "Un profil {0} existe déjà.", "Ein Profil {0} gibt es schon.") },
        { "defaultName", R("Main", "Principale", "Principal", "Principal", "Haupt") },
        { "noClaude", R("Claude Desktop is not installed. Get it from claude.ai/download, then open Claude Switcher again.",
                        "Claude Desktop non è installato. Scaricalo da claude.ai/download, poi riapri Claude Switcher.",
                        "Claude Desktop no está instalado. Descárgalo de claude.ai/download y vuelve a abrir Claude Switcher.",
                        "Claude Desktop n'est pas installé. Téléchargez-le sur claude.ai/download, puis rouvrez Claude Switcher.",
                        "Claude Desktop ist nicht installiert. Lade es von claude.ai/download und öffne Claude Switcher erneut.") },
        { "pinHint", R("To keep {0} on the taskbar, right-click its icon there and choose Pin to taskbar.",
                       "Per tenere {0} nella barra delle applicazioni, clic destro sulla sua icona e Aggiungi alla barra delle applicazioni.",
                       "Para dejar {0} en la barra de tareas, haz clic derecho en su icono y elige Anclar a la barra de tareas.",
                       "Pour garder {0} dans la barre des tâches, clic droit sur son icône puis Épingler à la barre des tâches.",
                       "Um {0} in der Taskleiste zu behalten, klicke mit der rechten Maustaste auf das Symbol und wähle An Taskleiste anheften.") },
        { "shortcutDesc", R("Claude Desktop, profile {0} (Claude Switcher)", "Claude Desktop, profilo {0} (Claude Switcher)",
                            "Claude Desktop, perfil {0} (Claude Switcher)", "Claude Desktop, profil {0} (Claude Switcher)",
                            "Claude Desktop, Profil {0} (Claude Switcher)") },
        { "Green", R("Green", "Verde", "Verde", "Vert", "Grün") },
        { "Blue", R("Blue", "Blu", "Azul", "Bleu", "Blau") },
        { "Purple", R("Purple", "Viola", "Morado", "Violet", "Lila") },
        { "Orange", R("Orange", "Arancione", "Naranja", "Orange", "Orange") },
        { "Red", R("Red", "Rosso", "Rojo", "Rouge", "Rot") },
        { "Yellow", R("Yellow", "Giallo", "Amarillo", "Jaune", "Gelb") },
        { "Pink", R("Pink", "Rosa", "Rosa", "Rose", "Rosa") },
        { "Teal", R("Teal", "Turchese", "Turquesa", "Turquoise", "Türkis") },
        { "Grey", R("Grey", "Grigio", "Gris", "Gris", "Grau") },
    };
}

}
