// Every string the user sees, in every language the app speaks.
// Kept in code rather than .lproj bundles because the same binary is copied into
// each profile's launcher, and a copied binary must carry its own words.
import Foundation

let languages = ["en", "it", "es", "fr", "de"]

/// First of the user's preferred languages that we have, else English.
let lang: String = {
    if let forced = ProcessInfo.processInfo.environment["CLAUDE_SWITCHER_LANG"], languages.contains(forced) { return forced }
    for l in Locale.preferredLanguages {
        let code = String(l.prefix(2))
        if languages.contains(code) { return code }
    }
    return "en"
}()

func t(_ key: String, _ args: CVarArg...) -> String {
    let row = strings[key] ?? [:]
    let s = row[lang] ?? row["en"] ?? key
    return args.isEmpty ? s : String(format: s, arguments: args)
}

private let strings: [String: [String: String]] = [
    "open": ["en": "Open", "it": "Apri", "es": "Abrir", "fr": "Ouvrir", "de": "Öffnen"],
    "front": ["en": "Bring to front", "it": "Porta davanti", "es": "Traer al frente", "fr": "Mettre au premier plan", "de": "In den Vordergrund"],
    "quitProfile": ["en": "Quit %@", "it": "Esci da %@", "es": "Salir de %@", "fr": "Quitter %@", "de": "%@ beenden"],
    "safeLogin": ["en": "Safe login (closes the others)", "it": "Login sicuro (chiude gli altri)", "es": "Inicio de sesión seguro (cierra los demás)",
                  "fr": "Connexion sûre (ferme les autres)", "de": "Sicher anmelden (schließt die anderen)"],
    "openAtLogin": ["en": "Open when the Mac starts", "it": "Apri all'avvio del Mac", "es": "Abrir al iniciar el Mac",
                    "fr": "Ouvrir au démarrage du Mac", "de": "Beim Start des Mac öffnen"],
    "showInDock": ["en": "Show its icon in the Dock", "it": "Mostra la sua icona nel Dock", "es": "Mostrar su icono en el Dock",
                   "fr": "Afficher son icône dans le Dock", "de": "Symbol im Dock zeigen"],
    "edit": ["en": "Edit…", "it": "Modifica…", "es": "Editar…", "fr": "Modifier…", "de": "Bearbeiten…"],
    "delete": ["en": "Delete profile…", "it": "Elimina profilo…", "es": "Eliminar perfil…", "fr": "Supprimer le profil…", "de": "Profil löschen…"],
    "manage": ["en": "Manage", "it": "Gestisci", "es": "Gestionar", "fr": "Gérer", "de": "Verwalten"],
    "newProfile": ["en": "New profile…", "it": "Nuovo profilo…", "es": "Nuevo perfil…", "fr": "Nouveau profil…", "de": "Neues Profil…"],
    "showMenuBar": ["en": "Show in the menu bar", "it": "Mostra nella barra dei menu", "es": "Mostrar en la barra de menús",
                    "fr": "Afficher dans la barre des menus", "de": "In der Menüleiste zeigen"],
    "switcherAtLogin": ["en": "Start Claude Switcher at login", "it": "Avvia Claude Switcher al login", "es": "Iniciar Claude Switcher al iniciar sesión",
                        "fr": "Lancer Claude Switcher à l'ouverture de session", "de": "Claude Switcher bei der Anmeldung starten"],
    "quitSwitcher": ["en": "Quit Claude Switcher", "it": "Esci da Claude Switcher", "es": "Salir de Claude Switcher",
                     "fr": "Quitter Claude Switcher", "de": "Claude Switcher beenden"],
    "sameAccount": ["en": "⚠︎ same account as %@", "it": "⚠︎ stesso account di %@", "es": "⚠︎ misma cuenta que %@",
                    "fr": "⚠︎ même compte que %@", "de": "⚠︎ gleiches Konto wie %@"],
    "loginTitle": ["en": "Log in to %@", "it": "Login %@", "es": "Iniciar sesión en %@", "fr": "Connexion à %@", "de": "Bei %@ anmelden"],
    "loginBody": ["en": "I will close %@ while you log in, so the login reaches the right profile.",
                  "it": "Chiudo %@ finché fai il login, così arriva al profilo giusto.",
                  "es": "Cierro %@ mientras inicias sesión, para que llegue al perfil correcto.",
                  "fr": "Je ferme %@ pendant la connexion, pour qu'elle arrive au bon profil.",
                  "de": "Ich schließe %@ während der Anmeldung, damit sie im richtigen Profil ankommt."],
    "closeContinue": ["en": "Close and continue", "it": "Chiudi e continua", "es": "Cerrar y continuar", "fr": "Fermer et continuer", "de": "Schließen und fortfahren"],
    "cancel": ["en": "Cancel", "it": "Annulla", "es": "Cancelar", "fr": "Annuler", "de": "Abbrechen"],
    "deleteTitle": ["en": "Delete the profile %@?", "it": "Eliminare il profilo %@?", "es": "¿Eliminar el perfil %@?",
                    "fr": "Supprimer le profil %@ ?", "de": "Profil %@ löschen?"],
    "deleteBody": ["en": "Its icon leaves the Dock and the menu. Nothing else is deleted: the account data stays in %@.",
                   "it": "L'icona sparisce dal Dock e dal menu. Nient'altro viene cancellato: i dati dell'account restano in %@.",
                   "es": "Su icono sale del Dock y del menú. No se borra nada más: los datos de la cuenta quedan en %@.",
                   "fr": "Son icône quitte le Dock et le menu. Rien d'autre n'est effacé : les données du compte restent dans %@.",
                   "de": "Das Symbol verschwindet aus Dock und Menü. Sonst wird nichts gelöscht: die Kontodaten bleiben in %@."],
    "defaultData": ["en": "Claude's own folder (default profile)", "it": "la cartella di Claude (profilo predefinito)",
                    "es": "la carpeta de Claude (perfil predeterminado)", "fr": "le dossier de Claude (profil par défaut)",
                    "de": "Claudes eigenem Ordner (Standardprofil)"],
    "deleteButton": ["en": "Delete", "it": "Elimina", "es": "Eliminar", "fr": "Supprimer", "de": "Löschen"],
    "newTitle": ["en": "New profile", "it": "Nuovo profilo", "es": "Nuevo perfil", "fr": "Nouveau profil", "de": "Neues Profil"],
    "editTitle": ["en": "Edit %@", "it": "Modifica %@", "es": "Editar %@", "fr": "Modifier %@", "de": "%@ bearbeiten"],
    "save": ["en": "Save", "it": "Salva", "es": "Guardar", "fr": "Enregistrer", "de": "Sichern"],
    "name": ["en": "Name", "it": "Nome", "es": "Nombre", "fr": "Nom", "de": "Name"],
    "letter": ["en": "Letter", "it": "Lettera", "es": "Letra", "fr": "Lettre", "de": "Buchstabe"],
    "color": ["en": "Color", "it": "Colore", "es": "Color", "fr": "Couleur", "de": "Farbe"],
    "email": ["en": "E-mail", "it": "E-mail", "es": "Correo", "fr": "E-mail", "de": "E-Mail"],
    "namePh": ["en": "Name (e.g. Work)", "it": "Nome (es. Work)", "es": "Nombre (p. ej. Trabajo)", "fr": "Nom (ex. Travail)", "de": "Name (z. B. Arbeit)"],
    "emailPh": ["en": "Account e-mail (just a reminder)", "it": "E-mail dell'account (solo promemoria)", "es": "Correo de la cuenta (solo recordatorio)",
                "fr": "E-mail du compte (simple rappel)", "de": "Konto-E-Mail (nur zur Erinnerung)"],
    "needName": ["en": "A name is required.", "it": "Serve un nome.", "es": "Hace falta un nombre.", "fr": "Il faut un nom.", "de": "Ein Name ist nötig."],
    "nameTaken": ["en": "A profile named %@ already exists.", "it": "Esiste già un profilo %@.", "es": "Ya existe un perfil %@.",
                  "fr": "Un profil %@ existe déjà.", "de": "Ein Profil %@ gibt es schon."],
    "defaultName": ["en": "Main", "it": "Principale", "es": "Principal", "fr": "Principal", "de": "Haupt"],
    "noClaude": ["en": "Claude Desktop is not installed in /Applications.", "it": "Claude Desktop non è installato in /Applications.",
                 "es": "Claude Desktop no está instalado en /Applications.", "fr": "Claude Desktop n'est pas installé dans /Applications.",
                 "de": "Claude Desktop ist nicht in /Applications installiert."],
    "Green": ["en": "Green", "it": "Verde", "es": "Verde", "fr": "Vert", "de": "Grün"],
    "Blue": ["en": "Blue", "it": "Blu", "es": "Azul", "fr": "Bleu", "de": "Blau"],
    "Purple": ["en": "Purple", "it": "Viola", "es": "Morado", "fr": "Violet", "de": "Lila"],
    "Orange": ["en": "Orange", "it": "Arancione", "es": "Naranja", "fr": "Orange", "de": "Orange"],
    "Red": ["en": "Red", "it": "Rosso", "es": "Rojo", "fr": "Rouge", "de": "Rot"],
    "Yellow": ["en": "Yellow", "it": "Giallo", "es": "Amarillo", "fr": "Jaune", "de": "Gelb"],
    "Pink": ["en": "Pink", "it": "Rosa", "es": "Rosa", "fr": "Rose", "de": "Rosa"],
    "Teal": ["en": "Teal", "it": "Turchese", "es": "Turquesa", "fr": "Turquoise", "de": "Türkis"],
    "Grey": ["en": "Grey", "it": "Grigio", "es": "Gris", "fr": "Gris", "de": "Grau"],
]
