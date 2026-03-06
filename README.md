# VoidTerminal

VoidTerminal est une application de chiffrement polyalphabétique et un gestionnaire de notes locales. Son moteur de substitution repose sur des règles créées par l'utilisateur : vous définissez vos propres séquences de décalage (ex: +3, +1, +4) et les assignez à des cibles typographiques (voyelles, consonnes, ou lettres spécifiques).

Lors du chiffrement, l'algorithme évolue dynamiquement en appliquant ces séquences au fil du texte. Étant donné que le décalage dépend de la nature de la lettre d'origine (qui est masquée une fois chiffrée), le déchiffrement génère plusieurs correspondances mathématiquement valides. L'application intègre donc un dictionnaire de résolution (à importer par l'utilisateur au premier lancement, puis enrichissable manuellement) qui filtre ces ambiguïtés pour retrouver le mot exact.

En complément, un module d'alphabet personnalisé permet de substituer visuellement les lettres par les symboles de votre choix. L'intégralité du profil (notes, dictionnaire personnel, paramètres des règles et alphabet personnalisé) est chiffrée et stockée localement en AES-256.
# Void
