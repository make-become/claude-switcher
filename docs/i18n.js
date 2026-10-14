// Page languages. The language is ?lang=…, else the last one picked, else the browser's, else English.
(() => {
const CMD = "curl -fsSL https://make-become.github.io/claude-switcher/install.sh | sh";
const NAMES = { en: "English", it: "Italiano", es: "Español", fr: "Français", de: "Deutsch" };

const T = {
en: {
  lead: "Use several Claude Desktop accounts at the same time on one Mac: personal, work, a client. Each one gets its own coloured Dock icon, its own login and its own shortcut, and it is never opened twice. Free, open source, unofficial.",
  altMenu: "The Claude Switcher menu with three profiles", altEdit: "Editing a profile: name, letter, colour, e-mail",
  installH: "Install", installP: "Paste this in the Terminal:",
  askH: "Or ask Claude to do it", askP: "Paste this in Claude Code or in Claude Desktop (with access to your Mac):",
  prompt: `Install Claude Switcher on my Mac: run \`${CMD}\` in the terminal, then tell me whether its menu opened. Details: https://make-become.github.io/claude-switcher/details.html`,
  copy: "Copy", copied: "Copied", more: "More info", unofficial: "A <a href="https://make-become.github.io/">make become</a> project. Unofficial, not affiliated with Anthropic.",
  back: "← Back", source: "Source code on GitHub",
  whatH: "What it does",
  whatList: `<li><strong>One profile per account.</strong> Each profile is its own Claude Desktop, signed in to its own account, open at the same time as the others.</li>
<li><strong>One Dock icon per profile,</strong> with a coloured letter. The dot under it means that profile is open; a click brings it to the front.</li>
<li><strong>Never two copies of one profile.</strong> An open profile is brought forward, not started again.</li>
<li><strong>A menu</strong> where your mouse is, in the Dock (right-click) and in the menu bar, with one dot per profile <span class="dots"><span class="g">●</span><span class="b">●</span><span class="o">○</span></span>, filled when open.</li>
<li><strong>Shortcuts:</strong> <code>⌥⌘1</code>, <code>⌥⌘2</code>, <code>⌥⌘3</code>… bring a profile forward from any app.</li>
<li><strong>Safe login</strong> closes the other profiles while you sign in (see below).</li>
<li><strong>Languages:</strong> English, Italiano, Español, Français, Deutsch, from your Mac's settings.</li>`,
  folderH: "Is a profile a folder on my disk?",
  folderP1: "Yes, and that is all it is. Claude Desktop keeps everything about your account (login, settings, caches) in one folder. A profile is just <em>another</em> such folder, so each account keeps its own things and none can see or overwrite the others.",
  folderP2: "Your first profile is Claude's normal folder, which Claude Switcher never touches. Each extra profile gets a folder next to it, created by Claude Desktop itself the first time that profile opens.",
  diskH: "Everything it puts on your Mac",
  diskIntro: "The complete list. Nothing is installed anywhere else, nothing is sent anywhere, and Claude.app itself is never modified.",
  thWhere: "Where", thWhat: "What it is",
  rApp: "The app. About 1 MB.",
  rLauncher: "One small launcher per profile: the icon you click in the Dock. About 1 MB each.",
  rDefault: "Claude's own folder, used by your first profile. Left as it is.",
  rExtra: "The data of each extra profile: its login and settings. Made by Claude Desktop, not by Claude Switcher.",
  rList: "The list of your profiles: name, letter, colour, the e-mail you typed. A few hundred bytes.",
  rAgent: "Only if you tick \"Open when the Mac starts\" for that profile. Untick it and the file goes away.",
  rDock: "A profile's icon is added when you create it and removed when you delete it or untick \"Show its icon in the Dock\".",
  deleteH: "What \"Delete profile\" does",
  deleteP: "It removes the profile's icon from the Dock and the menu, and moves its launcher to the Trash. It does <strong>not</strong> delete the profile's data folder: the account data stays, and a new profile with the same name finds it again. To erase it, drag the <code>Claude-&lt;Name&gt;</code> folder to the Trash yourself.",
  uninstallH: "Uninstalling",
  uninstallP: "Quit Claude Switcher, drag to the Trash the <code>Claude &lt;Name&gt;.app</code> launchers in <code>~/Applications</code> and <code>Claude Switcher.app</code> in <code>/Applications</code>. Your Claude accounts and their folders are left untouched.",
  loginH: "Why \"Safe login\"",
  loginP: "When you sign in, Claude Desktop sends you to the browser, and the browser hands the login back to \"Claude\". With several profiles open, macOS picks one of them, and it may not be the one you were signing in to. Safe login closes the other profiles for that moment, so there is only one place for the login to arrive.",
  installerH: "What the installer does",
  installerP: "It downloads the latest release from GitHub, puts <code>Claude Switcher.app</code> in <code>/Applications</code>, removes the download quarantine (the app is open source but not notarised by Apple, so macOS would otherwise refuse to open it) and opens it. Nothing else. You can <a href=\"install.sh\">read it</a> before running it.",
  manualP: "To install by hand: download the zip from the <a href=\"https://github.com/make-become/claude-switcher/releases/latest\">latest release</a>, move the app to Applications, and open it the first time with right-click → Open. To build it yourself: <code>git clone</code> the repository and run <code>./build.sh</code>. Needs macOS 13 or later and Claude Desktop in <code>/Applications</code>.",
  knowH: "Good to know",
  knowList: `<li>The e-mail next to a profile is a label you type: Claude Desktop does not store it where other apps can read it. If two profiles end up on the same account, the menu warns you.</li>
<li>Extra profiles are started with a standard launch option of this kind of app (<code>--user-data-dir</code>). Claude Desktop does not document it, so an update could change how it behaves.</li>
<li>In extra profiles Claude Desktop turns off pairing with the Claude browser extension.</li>
<li>While a profile is open you also see Claude Desktop's own icon in the Dock, next to the coloured one. Hiding it would mean modifying Claude.app, which breaks its signature and its updates, so Claude Switcher does not do it.</li>`,
  footer: "MIT licence · <a href="https://make-become.github.io/">make become</a> · Claude is a trademark of Anthropic; this project is not affiliated with Anthropic.",
},
it: {
  lead: "Usa più account di Claude Desktop insieme sullo stesso Mac: personale, lavoro, un cliente. Ognuno ha la sua icona colorata nel Dock, il suo login e la sua scorciatoia, e non si apre mai due volte. Gratuito, open source, non ufficiale.",
  altMenu: "Il menu di Claude Switcher con tre profili", altEdit: "Modifica di un profilo: nome, lettera, colore, e-mail",
  installH: "Installa", installP: "Incolla questo nel Terminale:",
  askH: "Oppure chiedi a Claude di farlo", askP: "Incolla questo in Claude Code o in Claude Desktop (con accesso al tuo Mac):",
  prompt: `Installa Claude Switcher sul mio Mac: esegui \`${CMD}\` nel terminale, poi dimmi se il suo menu si è aperto. Dettagli: https://make-become.github.io/claude-switcher/details.html`,
  copy: "Copia", copied: "Copiato", more: "Più informazioni", unofficial: "Un progetto <a href="https://make-become.github.io/">make become</a>. Non ufficiale, non affiliato ad Anthropic.",
  back: "← Indietro", source: "Codice sorgente su GitHub",
  whatH: "Cosa fa",
  whatList: `<li><strong>Un profilo per account.</strong> Ogni profilo è un Claude Desktop a sé, collegato al suo account, aperto insieme agli altri.</li>
<li><strong>Un'icona nel Dock per profilo,</strong> con una lettera colorata. Il pallino sotto vuol dire che quel profilo è aperto; un clic lo porta davanti.</li>
<li><strong>Mai due copie dello stesso profilo.</strong> Un profilo già aperto viene portato davanti, non riavviato.</li>
<li><strong>Un menu</strong> dove si trova il mouse, nel Dock (clic destro) e nella barra dei menu, con un pallino per profilo <span class="dots"><span class="g">●</span><span class="b">●</span><span class="o">○</span></span>, pieno quando è aperto.</li>
<li><strong>Scorciatoie:</strong> <code>⌥⌘1</code>, <code>⌥⌘2</code>, <code>⌥⌘3</code>… portano davanti un profilo da qualunque app.</li>
<li><strong>Login sicuro</strong> chiude gli altri profili mentre fai l'accesso (vedi sotto).</li>
<li><strong>Lingue:</strong> English, Italiano, Español, Français, Deutsch, secondo le impostazioni del Mac.</li>`,
  folderH: "Un profilo è una cartella sul disco?",
  folderP1: "Sì, ed è solo questo. Claude Desktop tiene tutto del tuo account (login, impostazioni, cache) in una cartella. Un profilo è semplicemente <em>un'altra</em> cartella così, quindi ogni account ha le sue cose e nessuno può vedere o sovrascrivere quelle degli altri.",
  folderP2: "Il primo profilo è la cartella normale di Claude, che Claude Switcher non tocca mai. Ogni profilo in più ha una cartella accanto, creata da Claude Desktop stesso la prima volta che quel profilo si apre.",
  diskH: "Tutto quello che mette sul tuo Mac",
  diskIntro: "L'elenco completo. Niente viene installato altrove, niente viene inviato da nessuna parte, e Claude.app non viene mai modificato.",
  thWhere: "Dove", thWhat: "Cos'è",
  rApp: "L'app. Circa 1 MB.",
  rLauncher: "Un piccolo lanciatore per profilo: l'icona che clicchi nel Dock. Circa 1 MB ciascuno.",
  rDefault: "La cartella di Claude, usata dal primo profilo. Lasciata com'è.",
  rExtra: "I dati di ogni profilo in più: login e impostazioni. Li crea Claude Desktop, non Claude Switcher.",
  rList: "L'elenco dei profili: nome, lettera, colore, l'e-mail che hai scritto. Poche centinaia di byte.",
  rAgent: "Solo se spunti \"Apri all'avvio del Mac\" per quel profilo. Togli la spunta e il file sparisce.",
  rDock: "L'icona di un profilo viene aggiunta quando lo crei e tolta quando lo elimini o togli la spunta a \"Mostra la sua icona nel Dock\".",
  deleteH: "Cosa fa \"Elimina profilo\"",
  deleteP: "Toglie l'icona del profilo dal Dock e dal menu e sposta il suo lanciatore nel Cestino. <strong>Non</strong> cancella la cartella dei dati: i dati dell'account restano, e un nuovo profilo con lo stesso nome li ritrova. Per cancellarli, trascina tu la cartella <code>Claude-&lt;Nome&gt;</code> nel Cestino.",
  uninstallH: "Disinstallare",
  uninstallP: "Esci da Claude Switcher, poi trascina nel Cestino i lanciatori <code>Claude &lt;Nome&gt;.app</code> da <code>~/Applications</code> e <code>Claude Switcher.app</code> da <code>/Applications</code>. I tuoi account Claude e le loro cartelle restano intatti.",
  loginH: "Perché \"Login sicuro\"",
  loginP: "Quando accedi, Claude Desktop ti manda nel browser, e il browser restituisce il login a \"Claude\". Con più profili aperti macOS ne sceglie uno, e potrebbe non essere quello in cui stavi entrando. Login sicuro chiude per un momento gli altri profili, così il login ha un solo posto dove arrivare.",
  installerH: "Cosa fa l'installer",
  installerP: "Scarica l'ultima release da GitHub, mette <code>Claude Switcher.app</code> in <code>/Applications</code>, toglie la quarantena del download (l'app è open source ma non notarizzata da Apple, quindi altrimenti macOS si rifiuterebbe di aprirla) e la apre. Nient'altro. Puoi <a href=\"install.sh\">leggerlo</a> prima di eseguirlo.",
  manualP: "Per installarla a mano: scarica lo zip dall'<a href=\"https://github.com/make-become/claude-switcher/releases/latest\">ultima release</a>, sposta l'app in Applicazioni e la prima volta aprila con clic destro → Apri. Per compilarla tu: <code>git clone</code> del repository e <code>./build.sh</code>. Serve macOS 13 o successivo e Claude Desktop in <code>/Applications</code>.",
  knowH: "Da sapere",
  knowList: `<li>L'e-mail accanto a un profilo è un'etichetta che scrivi tu: Claude Desktop non la salva dove altre app possano leggerla. Se due profili finiscono sullo stesso account, il menu ti avvisa.</li>
<li>I profili in più si avviano con un'opzione standard di questo tipo di app (<code>--user-data-dir</code>). Claude Desktop non la documenta, quindi un aggiornamento potrebbe cambiarne il comportamento.</li>
<li>Nei profili in più Claude Desktop disattiva il collegamento con l'estensione Claude per il browser.</li>
<li>Mentre un profilo è aperto vedi nel Dock anche l'icona di Claude Desktop, accanto a quella colorata. Nasconderla vorrebbe dire modificare Claude.app, rompendone la firma e gli aggiornamenti, quindi Claude Switcher non lo fa.</li>`,
  footer: "Licenza MIT · <a href="https://make-become.github.io/">make become</a> · Claude è un marchio di Anthropic; questo progetto non è affiliato ad Anthropic.",
},
es: {
  lead: "Usa varias cuentas de Claude Desktop a la vez en un mismo Mac: personal, trabajo, un cliente. Cada una tiene su propio icono de color en el Dock, su propio inicio de sesión y su propio atajo, y nunca se abre dos veces. Gratis, de código abierto, no oficial.",
  altMenu: "El menú de Claude Switcher con tres perfiles", altEdit: "Editar un perfil: nombre, letra, color, correo",
  installH: "Instalar", installP: "Pega esto en el Terminal:",
  askH: "O pídeselo a Claude", askP: "Pega esto en Claude Code o en Claude Desktop (con acceso a tu Mac):",
  prompt: `Instala Claude Switcher en mi Mac: ejecuta \`${CMD}\` en el terminal y dime si se abrió su menú. Detalles: https://make-become.github.io/claude-switcher/details.html`,
  copy: "Copiar", copied: "Copiado", more: "Más información", unofficial: "Un proyecto de <a href="https://make-become.github.io/">make become</a>. No oficial, sin relación con Anthropic.",
  back: "← Volver", source: "Código fuente en GitHub",
  whatH: "Qué hace",
  whatList: `<li><strong>Un perfil por cuenta.</strong> Cada perfil es su propio Claude Desktop, con su propia cuenta, abierto a la vez que los demás.</li>
<li><strong>Un icono en el Dock por perfil,</strong> con una letra de color. El punto debajo indica que ese perfil está abierto; un clic lo trae al frente.</li>
<li><strong>Nunca dos copias del mismo perfil.</strong> Un perfil abierto se trae al frente, no se vuelve a iniciar.</li>
<li><strong>Un menú</strong> donde está el ratón, en el Dock (clic derecho) y en la barra de menús, con un punto por perfil <span class="dots"><span class="g">●</span><span class="b">●</span><span class="o">○</span></span>, relleno cuando está abierto.</li>
<li><strong>Atajos:</strong> <code>⌥⌘1</code>, <code>⌥⌘2</code>, <code>⌥⌘3</code>… traen un perfil al frente desde cualquier app.</li>
<li><strong>Inicio de sesión seguro</strong> cierra los otros perfiles mientras entras (ver abajo).</li>
<li><strong>Idiomas:</strong> English, Italiano, Español, Français, Deutsch, según los ajustes del Mac.</li>`,
  folderH: "¿Un perfil es una carpeta en mi disco?",
  folderP1: "Sí, y no es más que eso. Claude Desktop guarda todo lo de tu cuenta (sesión, ajustes, cachés) en una carpeta. Un perfil es simplemente <em>otra</em> carpeta así, de modo que cada cuenta tiene lo suyo y ninguna puede ver ni sobrescribir lo de las demás.",
  folderP2: "Tu primer perfil es la carpeta normal de Claude, que Claude Switcher nunca toca. Cada perfil extra tiene una carpeta al lado, creada por el propio Claude Desktop la primera vez que se abre ese perfil.",
  diskH: "Todo lo que pone en tu Mac",
  diskIntro: "La lista completa. No se instala nada en otro sitio, no se envía nada a ningún lado y Claude.app nunca se modifica.",
  thWhere: "Dónde", thWhat: "Qué es",
  rApp: "La app. Alrededor de 1 MB.",
  rLauncher: "Un pequeño lanzador por perfil: el icono que pulsas en el Dock. Alrededor de 1 MB cada uno.",
  rDefault: "La carpeta de Claude, usada por tu primer perfil. Se deja como está.",
  rExtra: "Los datos de cada perfil extra: su sesión y sus ajustes. Los crea Claude Desktop, no Claude Switcher.",
  rList: "La lista de tus perfiles: nombre, letra, color, el correo que escribiste. Unos cientos de bytes.",
  rAgent: "Solo si marcas \"Abrir al iniciar el Mac\" en ese perfil. Desmárcalo y el archivo desaparece.",
  rDock: "El icono de un perfil se añade al crearlo y se quita al eliminarlo o al desmarcar \"Mostrar su icono en el Dock\".",
  deleteH: "Qué hace \"Eliminar perfil\"",
  deleteP: "Quita el icono del perfil del Dock y del menú y mueve su lanzador a la Papelera. <strong>No</strong> borra la carpeta de datos: los datos de la cuenta se quedan, y un perfil nuevo con el mismo nombre los vuelve a encontrar. Para borrarlos, arrastra tú la carpeta <code>Claude-&lt;Nombre&gt;</code> a la Papelera.",
  uninstallH: "Desinstalar",
  uninstallP: "Sal de Claude Switcher y arrastra a la Papelera los lanzadores <code>Claude &lt;Nombre&gt;.app</code> de <code>~/Applications</code> y <code>Claude Switcher.app</code> de <code>/Applications</code>. Tus cuentas de Claude y sus carpetas quedan intactas.",
  loginH: "Por qué \"Inicio de sesión seguro\"",
  loginP: "Al iniciar sesión, Claude Desktop te lleva al navegador, y el navegador devuelve la sesión a \"Claude\". Con varios perfiles abiertos, macOS elige uno, y puede no ser el que estabas usando. El inicio de sesión seguro cierra un momento los demás perfiles, para que la sesión solo tenga un sitio al que llegar.",
  installerH: "Qué hace el instalador",
  installerP: "Descarga la última versión de GitHub, pone <code>Claude Switcher.app</code> en <code>/Applications</code>, quita la cuarentena de la descarga (la app es de código abierto pero no está notarizada por Apple, así que macOS se negaría a abrirla) y la abre. Nada más. Puedes <a href=\"install.sh\">leerlo</a> antes de ejecutarlo.",
  manualP: "Para instalar a mano: descarga el zip de la <a href=\"https://github.com/make-become/claude-switcher/releases/latest\">última versión</a>, mueve la app a Aplicaciones y ábrela la primera vez con clic derecho → Abrir. Para compilarla: <code>git clone</code> del repositorio y <code>./build.sh</code>. Requiere macOS 13 o posterior y Claude Desktop en <code>/Applications</code>.",
  knowH: "Conviene saber",
  knowList: `<li>El correo junto a un perfil es una etiqueta que escribes tú: Claude Desktop no lo guarda donde otras apps puedan leerlo. Si dos perfiles acaban en la misma cuenta, el menú te avisa.</li>
<li>Los perfiles extra se inician con una opción estándar de este tipo de apps (<code>--user-data-dir</code>). Claude Desktop no la documenta, así que una actualización podría cambiar su comportamiento.</li>
<li>En los perfiles extra Claude Desktop desactiva el emparejamiento con la extensión de Claude para el navegador.</li>
<li>Mientras un perfil está abierto también ves en el Dock el icono de Claude Desktop, junto al de color. Ocultarlo exigiría modificar Claude.app, rompiendo su firma y sus actualizaciones, así que Claude Switcher no lo hace.</li>`,
  footer: "Licencia MIT · <a href="https://make-become.github.io/">make become</a> · Claude es una marca de Anthropic; este proyecto no está afiliado a Anthropic.",
},
fr: {
  lead: "Utilisez plusieurs comptes Claude Desktop en même temps sur un seul Mac : perso, travail, un client. Chacun a sa propre icône colorée dans le Dock, sa propre connexion et son propre raccourci, et n'est jamais ouvert deux fois. Gratuit, open source, non officiel.",
  altMenu: "Le menu de Claude Switcher avec trois profils", altEdit: "Modifier un profil : nom, lettre, couleur, e-mail",
  installH: "Installer", installP: "Collez ceci dans le Terminal :",
  askH: "Ou demandez à Claude de le faire", askP: "Collez ceci dans Claude Code ou dans Claude Desktop (avec accès à votre Mac) :",
  prompt: `Installe Claude Switcher sur mon Mac : exécute \`${CMD}\` dans le terminal, puis dis-moi si son menu s'est ouvert. Détails : https://make-become.github.io/claude-switcher/details.html`,
  copy: "Copier", copied: "Copié", more: "Plus d'infos", unofficial: "Un projet <a href="https://make-become.github.io/">make become</a>. Non officiel, sans lien avec Anthropic.",
  back: "← Retour", source: "Code source sur GitHub",
  whatH: "Ce qu'il fait",
  whatList: `<li><strong>Un profil par compte.</strong> Chaque profil est son propre Claude Desktop, connecté à son propre compte, ouvert en même temps que les autres.</li>
<li><strong>Une icône dans le Dock par profil,</strong> avec une lettre colorée. Le point dessous signifie que ce profil est ouvert ; un clic le met au premier plan.</li>
<li><strong>Jamais deux copies d'un même profil.</strong> Un profil ouvert est mis au premier plan, pas relancé.</li>
<li><strong>Un menu</strong> là où est la souris, dans le Dock (clic droit) et dans la barre des menus, avec un point par profil <span class="dots"><span class="g">●</span><span class="b">●</span><span class="o">○</span></span>, plein quand il est ouvert.</li>
<li><strong>Raccourcis :</strong> <code>⌥⌘1</code>, <code>⌥⌘2</code>, <code>⌥⌘3</code>… mettent un profil au premier plan depuis n'importe quelle app.</li>
<li><strong>Connexion sûre</strong> ferme les autres profils pendant que vous vous connectez (voir plus bas).</li>
<li><strong>Langues :</strong> English, Italiano, Español, Français, Deutsch, selon les réglages du Mac.</li>`,
  folderH: "Un profil, c'est un dossier sur mon disque ?",
  folderP1: "Oui, et rien de plus. Claude Desktop garde tout ce qui concerne votre compte (connexion, réglages, caches) dans un dossier. Un profil n'est qu'<em>un autre</em> dossier de ce type : chaque compte garde ses affaires et aucun ne peut voir ou écraser celles des autres.",
  folderP2: "Votre premier profil est le dossier normal de Claude, auquel Claude Switcher ne touche jamais. Chaque profil supplémentaire a un dossier à côté, créé par Claude Desktop lui-même à la première ouverture de ce profil.",
  diskH: "Tout ce qu'il met sur votre Mac",
  diskIntro: "La liste complète. Rien n'est installé ailleurs, rien n'est envoyé nulle part, et Claude.app n'est jamais modifié.",
  thWhere: "Où", thWhat: "Ce que c'est",
  rApp: "L'app. Environ 1 Mo.",
  rLauncher: "Un petit lanceur par profil : l'icône sur laquelle vous cliquez dans le Dock. Environ 1 Mo chacun.",
  rDefault: "Le dossier de Claude, utilisé par votre premier profil. Laissé tel quel.",
  rExtra: "Les données de chaque profil supplémentaire : connexion et réglages. Créées par Claude Desktop, pas par Claude Switcher.",
  rList: "La liste de vos profils : nom, lettre, couleur, l'e-mail saisi. Quelques centaines d'octets.",
  rAgent: "Seulement si vous cochez « Ouvrir au démarrage du Mac » pour ce profil. Décochez et le fichier disparaît.",
  rDock: "L'icône d'un profil est ajoutée à sa création et retirée quand vous le supprimez ou décochez « Afficher son icône dans le Dock ».",
  deleteH: "Ce que fait « Supprimer le profil »",
  deleteP: "Il retire l'icône du profil du Dock et du menu, et met son lanceur à la Corbeille. Il ne supprime <strong>pas</strong> le dossier de données : les données du compte restent, et un nouveau profil du même nom les retrouve. Pour les effacer, glissez vous-même le dossier <code>Claude-&lt;Nom&gt;</code> dans la Corbeille.",
  uninstallH: "Désinstaller",
  uninstallP: "Quittez Claude Switcher, puis glissez dans la Corbeille les lanceurs <code>Claude &lt;Nom&gt;.app</code> de <code>~/Applications</code> et <code>Claude Switcher.app</code> de <code>/Applications</code>. Vos comptes Claude et leurs dossiers restent intacts.",
  loginH: "Pourquoi « Connexion sûre »",
  loginP: "Pour vous connecter, Claude Desktop vous envoie dans le navigateur, qui renvoie ensuite la connexion à « Claude ». Avec plusieurs profils ouverts, macOS en choisit un, et ce n'est peut-être pas le bon. La connexion sûre ferme un instant les autres profils, pour que la connexion n'ait qu'un seul endroit où arriver.",
  installerH: "Ce que fait l'installeur",
  installerP: "Il télécharge la dernière version depuis GitHub, place <code>Claude Switcher.app</code> dans <code>/Applications</code>, retire la quarantaine du téléchargement (l'app est open source mais pas notariée par Apple, sinon macOS refuserait de l'ouvrir) et l'ouvre. Rien d'autre. Vous pouvez <a href=\"install.sh\">le lire</a> avant de l'exécuter.",
  manualP: "Pour installer à la main : téléchargez le zip de la <a href=\"https://github.com/make-become/claude-switcher/releases/latest\">dernière version</a>, déplacez l'app dans Applications et ouvrez-la la première fois avec clic droit → Ouvrir. Pour la compiler : <code>git clone</code> du dépôt puis <code>./build.sh</code>. Nécessite macOS 13 ou plus récent et Claude Desktop dans <code>/Applications</code>.",
  knowH: "Bon à savoir",
  knowList: `<li>L'e-mail à côté d'un profil est une étiquette que vous saisissez : Claude Desktop ne le stocke pas là où d'autres apps peuvent le lire. Si deux profils se retrouvent sur le même compte, le menu vous prévient.</li>
<li>Les profils supplémentaires sont lancés avec une option standard de ce type d'app (<code>--user-data-dir</code>). Claude Desktop ne la documente pas, une mise à jour pourrait donc changer son comportement.</li>
<li>Dans les profils supplémentaires, Claude Desktop désactive le jumelage avec l'extension Claude pour le navigateur.</li>
<li>Quand un profil est ouvert, vous voyez aussi l'icône de Claude Desktop dans le Dock, à côté de l'icône colorée. La masquer obligerait à modifier Claude.app, ce qui casse sa signature et ses mises à jour : Claude Switcher ne le fait donc pas.</li>`,
  footer: "Licence MIT · <a href="https://make-become.github.io/">make become</a> · Claude est une marque d'Anthropic ; ce projet n'est pas affilié à Anthropic.",
},
de: {
  lead: "Mehrere Claude-Desktop-Konten gleichzeitig auf einem Mac: privat, Arbeit, ein Kunde. Jedes hat sein eigenes farbiges Dock-Symbol, seine eigene Anmeldung und sein eigenes Tastenkürzel, und es wird nie doppelt geöffnet. Kostenlos, Open Source, inoffiziell.",
  altMenu: "Das Menü von Claude Switcher mit drei Profilen", altEdit: "Ein Profil bearbeiten: Name, Buchstabe, Farbe, E-Mail",
  installH: "Installieren", installP: "Das hier ins Terminal einfügen:",
  askH: "Oder Claude darum bitten", askP: "Das hier in Claude Code oder Claude Desktop (mit Zugriff auf deinen Mac) einfügen:",
  prompt: `Installiere Claude Switcher auf meinem Mac: führe \`${CMD}\` im Terminal aus und sag mir dann, ob sich sein Menü geöffnet hat. Details: https://make-become.github.io/claude-switcher/details.html`,
  copy: "Kopieren", copied: "Kopiert", more: "Mehr Infos", unofficial: "Ein Projekt von <a href="https://make-become.github.io/">make become</a>. Inoffiziell, nicht mit Anthropic verbunden.",
  back: "← Zurück", source: "Quellcode auf GitHub",
  whatH: "Was es macht",
  whatList: `<li><strong>Ein Profil pro Konto.</strong> Jedes Profil ist ein eigenes Claude Desktop mit eigenem Konto, gleichzeitig mit den anderen geöffnet.</li>
<li><strong>Ein Dock-Symbol pro Profil,</strong> mit farbigem Buchstaben. Der Punkt darunter heißt: dieses Profil ist offen; ein Klick holt es nach vorn.</li>
<li><strong>Nie zwei Kopien desselben Profils.</strong> Ein offenes Profil wird nach vorn geholt, nicht neu gestartet.</li>
<li><strong>Ein Menü</strong> dort, wo die Maus ist, im Dock (Rechtsklick) und in der Menüleiste, mit einem Punkt pro Profil <span class="dots"><span class="g">●</span><span class="b">●</span><span class="o">○</span></span>, gefüllt, wenn es offen ist.</li>
<li><strong>Tastenkürzel:</strong> <code>⌥⌘1</code>, <code>⌥⌘2</code>, <code>⌥⌘3</code>… holen ein Profil aus jeder App nach vorn.</li>
<li><strong>Sicher anmelden</strong> schließt die anderen Profile, während du dich anmeldest (siehe unten).</li>
<li><strong>Sprachen:</strong> English, Italiano, Español, Français, Deutsch, nach den Einstellungen des Mac.</li>`,
  folderH: "Ist ein Profil ein Ordner auf meiner Festplatte?",
  folderP1: "Ja, und mehr nicht. Claude Desktop speichert alles zu deinem Konto (Anmeldung, Einstellungen, Caches) in einem Ordner. Ein Profil ist einfach <em>noch ein</em> solcher Ordner: Jedes Konto behält seine Sachen, und keines kann die der anderen sehen oder überschreiben.",
  folderP2: "Dein erstes Profil ist Claudes normaler Ordner, den Claude Switcher nie anfasst. Jedes weitere Profil bekommt einen Ordner daneben, den Claude Desktop selbst beim ersten Öffnen anlegt.",
  diskH: "Alles, was es auf deinem Mac ablegt",
  diskIntro: "Die vollständige Liste. Nichts wird woanders installiert, nichts wird irgendwohin gesendet, und Claude.app wird nie verändert.",
  thWhere: "Wo", thWhat: "Was es ist",
  rApp: "Die App. Etwa 1 MB.",
  rLauncher: "Ein kleiner Starter pro Profil: das Symbol, das du im Dock anklickst. Je etwa 1 MB.",
  rDefault: "Claudes eigener Ordner, genutzt vom ersten Profil. Bleibt, wie er ist.",
  rExtra: "Die Daten jedes weiteren Profils: Anmeldung und Einstellungen. Angelegt von Claude Desktop, nicht von Claude Switcher.",
  rList: "Die Liste deiner Profile: Name, Buchstabe, Farbe, die eingegebene E-Mail. Ein paar hundert Bytes.",
  rAgent: "Nur wenn du bei dem Profil „Beim Start des Mac öffnen“ anhakst. Haken weg, Datei weg.",
  rDock: "Das Symbol eines Profils wird beim Anlegen hinzugefügt und beim Löschen oder mit „Symbol im Dock zeigen“ entfernt.",
  deleteH: "Was „Profil löschen“ macht",
  deleteP: "Es entfernt das Symbol des Profils aus Dock und Menü und legt seinen Starter in den Papierkorb. Den Datenordner löscht es <strong>nicht</strong>: Die Kontodaten bleiben, und ein neues Profil mit demselben Namen findet sie wieder. Zum Löschen ziehst du den Ordner <code>Claude-&lt;Name&gt;</code> selbst in den Papierkorb.",
  uninstallH: "Deinstallieren",
  uninstallP: "Claude Switcher beenden, dann die Starter <code>Claude &lt;Name&gt;.app</code> aus <code>~/Applications</code> und <code>Claude Switcher.app</code> aus <code>/Applications</code> in den Papierkorb ziehen. Deine Claude-Konten und ihre Ordner bleiben unberührt.",
  loginH: "Warum „Sicher anmelden“",
  loginP: "Beim Anmelden schickt Claude Desktop dich in den Browser, und der Browser gibt die Anmeldung an „Claude“ zurück. Sind mehrere Profile offen, wählt macOS eines aus, und das ist vielleicht nicht das richtige. Sicher anmelden schließt kurz die anderen Profile, damit die Anmeldung nur an einem Ort ankommen kann.",
  installerH: "Was der Installer macht",
  installerP: "Er lädt die neueste Version von GitHub, legt <code>Claude Switcher.app</code> in <code>/Applications</code> ab, entfernt die Download-Quarantäne (die App ist Open Source, aber nicht von Apple notarisiert, sonst würde macOS sie nicht öffnen) und öffnet sie. Sonst nichts. Du kannst ihn <a href=\"install.sh\">lesen</a>, bevor du ihn ausführst.",
  manualP: "Von Hand installieren: das Zip der <a href=\"https://github.com/make-become/claude-switcher/releases/latest\">neuesten Version</a> laden, die App in Programme verschieben und beim ersten Mal per Rechtsklick → Öffnen starten. Selbst bauen: Repository per <code>git clone</code> holen und <code>./build.sh</code> ausführen. Braucht macOS 13 oder neuer und Claude Desktop in <code>/Applications</code>.",
  knowH: "Gut zu wissen",
  knowList: `<li>Die E-Mail neben einem Profil ist ein Etikett, das du eingibst: Claude Desktop speichert sie nicht so, dass andere Apps sie lesen können. Landen zwei Profile auf demselben Konto, warnt dich das Menü.</li>
<li>Weitere Profile werden mit einer Standard-Startoption solcher Apps gestartet (<code>--user-data-dir</code>). Claude Desktop dokumentiert sie nicht, ein Update könnte das Verhalten also ändern.</li>
<li>In weiteren Profilen schaltet Claude Desktop die Kopplung mit der Claude-Browsererweiterung ab.</li>
<li>Solange ein Profil offen ist, siehst du im Dock auch das Symbol von Claude Desktop neben dem farbigen. Es auszublenden hieße, Claude.app zu verändern, was Signatur und Updates kaputt macht, deshalb tut Claude Switcher das nicht.</li>`,
  footer: "MIT-Lizenz · <a href="https://make-become.github.io/">make become</a> · Claude ist eine Marke von Anthropic; dieses Projekt ist nicht mit Anthropic verbunden.",
},
};

function pick() {
  const q = new URLSearchParams(location.search).get("lang");
  if (q && T[q]) return q;
  try { const s = localStorage.getItem("lang"); if (s && T[s]) return s; } catch (e) {}
  for (const l of navigator.languages || [navigator.language || "en"]) { const c = l.slice(0, 2); if (T[c]) return c; }
  return "en";
}

const lang = pick();
const s = T[lang];
document.documentElement.lang = lang;
document.querySelectorAll("[data-i18n]").forEach(el => { const v = s[el.dataset.i18n]; if (v !== undefined) el.innerHTML = v; });
document.querySelectorAll("[data-i18n-alt]").forEach(el => { el.alt = s[el.dataset.i18nAlt] || ""; });
document.querySelectorAll("[data-i18n-src]").forEach(el => { el.src = el.dataset.i18nSrc.replace("{lang}", lang); });
document.querySelectorAll("[data-keep-lang]").forEach(a => { a.href = a.getAttribute("href").split("?")[0] + "?lang=" + lang; });

// The prompt is shown as text; inline `code` marks stay as typed so the copy is exactly what Claude should read.
document.querySelectorAll("[data-copy]").forEach(b => b.addEventListener("click", async () => {
  const text = document.getElementById(b.dataset.copy).textContent;
  try { await navigator.clipboard.writeText(text); }
  catch (e) { const r = document.createRange(); r.selectNodeContents(document.getElementById(b.dataset.copy)); const sel = getSelection(); sel.removeAllRanges(); sel.addRange(r); document.execCommand("copy"); }
  b.textContent = s.copied; setTimeout(() => { b.textContent = s.copy; }, 1500);
}));

const box = document.querySelector(".langs");
if (box) for (const [code, name] of Object.entries(NAMES)) {
  const a = document.createElement("a");
  a.href = "?lang=" + code; a.textContent = name;
  if (code === lang) a.setAttribute("aria-current", "true");
  a.addEventListener("click", () => { try { localStorage.setItem("lang", code); } catch (e) {} });
  box.appendChild(a);
}
})();
