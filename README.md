# VoidTerminal

VoidTerminal est une application de chiffrement polyalphabétique et un gestionnaire de notes locales. Son moteur de substitution repose sur des règles créées par l'utilisateur : vous définissez vos propres séquences de décalage (ex: +3, +1, +4) et les assignez à des cibles typographiques (voyelles, consonnes, ou lettres spécifiques).

Lors du chiffrement, l'algorithme évolue dynamiquement en appliquant ces séquences au fil du texte. Étant donné que le décalage dépend de la nature de la lettre d'origine (qui est masquée une fois chiffrée), le déchiffrement génère plusieurs correspondances mathématiquement valides. L'application intègre donc un dictionnaire de résolution (à importer par l'utilisateur au premier lancement, puis enrichissable manuellement) qui filtre ces ambiguïtés pour retrouver le mot exact.

En complément, un module d'alphabet personnalisé permet de substituer visuellement les lettres par les symboles de votre choix. L'intégralité du profil (notes, dictionnaire personnel, paramètres des règles et alphabet personnalisé) est chiffrée et stockée localement en AES-256.

-----

## Aperçu de l'interface
![Modes d'affichage](assets/screenshots/ui-modes.png)

![Interface principale](assets/screenshots/ui-overview.png)

1. Réglages
2. Gestionnaire de notes
3. Séquence d'entrée
4. Encrypter / Décrypter
5. Sélecteur de mode (Mode 1 / Mode Void)
6. Résultat
7. Modes d'affichage (Glass / Ghost)
8. Verrouillage de l'application
9. Statut du profil chiffré
10. Ajout manuel au dictionnaire

## Installation

| Version | Taille | Prérequis |
|---------|--------|-----------|
| [VoidTerminal_v1.0.0.zip](https://github.com/ByronlLove/VoidTerminal/releases/tag/v1.0.0) | 197 Ko | [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |
| [VoidTerminal_v1.0.0_win-x64.zip](https://github.com/ByronlLove/VoidTerminal/releases/tag/v1.0.0) | 60.4 MB | Aucun |

1. Téléchargez l'archive de votre choix
2. Extrayez et exécutez `Void.exe`
3. Définissez votre mot de passe maître (clé AES-256)
4. Importez un dictionnaire `.txt` — source française recommandée : [French Wordlist par Taknok](https://github.com/Taknok/French-Wordlist/blob/master/francais.txt)



