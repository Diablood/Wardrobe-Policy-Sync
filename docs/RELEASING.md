# Publication de Wardrobe Policy Sync

## Version publiée sur GitHub : 1.1.0

Publication effectuée le 27 novembre 2026 :
[release 1.1.0](https://github.com/Diablood/Wardrobe-Policy-Sync/releases/tag/1.1.0).
Branche `main` et tag annoté `1.1.0` poussés ; le tag désigne le commit
`185f1940ceca86c4f43a1788a04c60b9043c655b`.
ZIP et fichier SHA-256 joints, tailles et empreintes distantes vérifiées.

SHA-256 du ZIP :
`1a6aafdfda7be5f5eb485b217e0de2e65ad174832db8378225006d9e58c583a1`.

Correctif de conditionnement du 27 novembre 2026 : aperçu réduit de
1 819 921 à 971 059 octets (960 × 640), PNG contrôlé visuellement. Le ZIP
et son empreinte ont été remplacés dans la release 1.1.0 ; le tag initial
reste inchangé. Les DLL déjà validées sont conservées à l'identique.
Le correctif de visuel et son contrôle de taille sont suivis sur `main`.
Le nouvel essai Workshop par l'utilisateur reste à confirmer.

La copie locale dans
`D:/SteamLibrary/steamapps/common/RimWorld/Mods/Wardrobe-Policy-Sync`
est prête pour l'envoi Workshop par l'utilisateur : 13 fichiers runtime
identiques au paquet publié, version 1.1.0, identifiant `3709575150` conservé.
Aucun envoi Workshop effectué par l'assistant.

La version du projet C# est la référence pour le numéro de version.
Le tag suit le format existant : `1.1.0`, sans préfixe `v`.
La DLL 1.6 reste celle du tag `1.0.0` ; seule la DLL 1.7 porte la nouvelle
version d'assembly. Ne pas reconstruire la DLL 1.6 contre le jeu 1.7.

`About/About.xml` porte aussi `modVersion=1.1.0` pour le jeu et RimSort,
ainsi qu'une mention visible dans la description utilisée sur le Workshop.
Le contrôle de cohérence vérifie ces valeurs contre le projet C#.
Le champ `modVersion` a été confirmé dans l'assembly du jeu 1.7 et dans
le [parseur officiel RimSort](https://github.com/RimSort/RimSort/blob/main/app/models/metadata/metadata_factory.py).

## Préparer le paquet

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./tools/package-release.ps1
```

Paramètres disponibles : `-RimWorldDir` et `-HarmonyPath`.
Pour un correctif de ressources uniquement, `-NoBuild` permet de réutiliser
les DLL déjà compilées et validées ; les contrôles et la vérification du ZIP
restent exécutés. Ne pas utiliser cette option après une modification C#.
Le script force une recompilation Release, exécute les contrôles de cohérence,
vérifie le binaire 1.6 contre le tag `1.0.0`, puis crée :

- `artifacts/releases/Wardrobe-Policy-Sync-1.1.0.zip`
- `artifacts/releases/Wardrobe-Policy-Sync-1.1.0.zip.sha256`

Le ZIP contient un dossier `Wardrobe-Policy-Sync` avec les ressources runtime,
README, changelog et un fichier de version. Chaque entrée est vérifiée par
SHA-256 contre le contenu préparé. Sources, outils, PDB et dépendances de build
ne sont pas inclus. L'identifiant Workshop `3709575150` est conservé.
Le contrôle de cohérence refuse un aperçu de 1 000 000 octets ou plus,
comme budget conservateur pour le Workshop. Vérifier le fichier effectivement
déployé, puis sa copie dans le paquet ; la taille du ZIP n'est pas ce contrôle.

Les résultats des contrôles et le retour utilisateur sont consignés dans
`TESTING.md`. Aucune validation supplémentaire sous 1.6 n'est revendiquée.

## Publication, sur demande explicite

La préparation et le tag local ne publient rien. Pour une publication demandée :

1. Vérifier que le commit et le tag `1.1.0` désignent le contenu final, que le
   dépôt est propre et que le paquet a passé les contrôles ci-dessus.
2. Pousser la branche et le tag vers origin, puis vérifier les références
   distantes avant de créer une release GitHub et joindre le ZIP et son SHA-256.
3. Pour le Workshop, fermer RimWorld et déployer la version finale avec
   `tools/deploy-test.ps1`, puis vérifier le dossier copié et l'identifiant
   Workshop avant la mise à jour de l'article existant. Ne pas créer un doublon.
4. Utiliser les notes ci-dessous et le visuel `About/Preview.png`. Vérifier
   après publication l'article Workshop et la release GitHub concernés.

## Notes de mise à jour prêtes à copier

Wardrobe Policy Sync 1.1.0

- Added RimWorld 1.7 support; RimWorld 1.6 remains supported with its existing binary.
- Fixed HAR child apparel restrictions on the shared outfit stand.
- Added a fallback for missing HAR race-command icons.
- Preserved apparel policy exclusions and existing save data.
- Updated the RimWorld 1.7 preview.

Requires Harmony and Odyssey. Humanoid Alien Races is optional.
