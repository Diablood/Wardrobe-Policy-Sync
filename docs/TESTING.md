# Validation RimWorld 1.7

## Contrôles exécutés

Reprise locale du 27 novembre 2026 : dépôt propre au commit `abe7b7c` avant
modification, aucun AGENTS.md ni procédure de publication présents. Paquet
`Wardrobe-Policy-Sync-RimWorld-1.7.zip` fourni dans la conversation précédente
appliqué après inspection. Les ressources communes ont été déplacées et les
anciens fichiers runtime racine retirés ; aucune modification locale préexistante.
Le visuel fourni portant la mention 1.7 a été inspecté.

Les vérifications techniques ci-dessous ont été relancées dans ce dépôt.
Les tests en jeu ont ensuite été confirmés par l'utilisateur (voir ci-dessous).

- Compilation Release contre RimWorld 1.7.5021 rev1365 : succès, 0 erreur,
  0 avertissement après passage à PatchAll(Assembly).
- HAR installé : Comp_OutfitStandHAR et propriété IsJuvenileBodyType vérifiés.
- Cause confirmée dans HAR 1.7 : son XML masque Building_KidOutfitStand ;
  son setter BodyType change uniquement le filtre modifiable du stand.
- Le gizmo de race de HAR utilise directement ThingDef.uiIcon.
- DLL 1.6 conservée à l'identique ; clés Scribe et defNames inchangés.
- XML : 5 fichiers parsés ; XPath HAR : une cible vérifiée, suppression simulée avec succès.
- Dossiers chargés : Common + version ; DLL 1.6 vérifiée par SHA-256.
- Aucun test en jeu réalisé par l'assistant. Aucune publication.

Commandes reproductibles :

```powershell
./build.cmd "D:/SteamLibrary/steamapps/common/RimWorld"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./tools/check-consistency.ps1 -RimWorldDir "D:/SteamLibrary/steamapps/common/RimWorld"
git diff --check
```

La compilation force `Rebuild`. Le script contrôle aussi la présence d'une
seule DLL par version et la parité des clés et arguments de traduction EN/FR.
Résultat local : 5 XML valides, 17 clés EN/FR et leurs arguments concordants,
une cible XPath HAR supprimée en simulation ; `git diff --check` réussi.
Les quatre cibles Harmony vanilla ont été retrouvées sans ambiguïté dans
Assembly-CSharp 1.7 ; le binaire reconstruit cible bien .NET 10.
La DLL 1.6 est une conservation du binaire existant, pas une recompilation
ni une validation en jeu sous 1.6. Les types, méthodes et IL HAR ont été
inspectés dans la DLL 1.7 installée, sans exécution du jeu.

## Déploiement local de test

Le 27 novembre 2026, déploiement demandé et effectué dans
`D:/SteamLibrary/steamapps/common/RimWorld/Mods/Wardrobe-Policy-Sync`.
Le jeu était fermé et aucune installation locale précédente n'était présente
à cet emplacement. Recompilation réussie (0 erreur, 0 avertissement), contrôles
de cohérence réussis, puis 13 fichiers runtime copiés et vérifiés par SHA-256.
Les sources, scripts, fichiers de debug et artefacts de build ne sont pas copiés.

Commande reproductible (chemins configurables, sauvegarde et restauration
d'une installation existante en cas d'échec) :

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./tools/deploy-test.ps1
```

Le mod doit être activé dans le jeu avec Harmony et Odyssey, après HAR
si présent. Éviter d'activer simultanément la copie Workshop du même mod.
Ce déploiement seul ne constitue pas une validation en jeu.

## Validation en jeu rapportée

Le 27 novembre 2026, après le déploiement local de test, l'utilisateur a
confirmé : « Tests ok ». Validation en jeu de la version 1.7 déployée
rapportée par l'utilisateur ; aucun problème signalé.
Le retour ne détaille pas les scénarios ni la configuration de mods testés ;
les cas individuels ci-dessous et les régressions facultatives ne disposent
pas de résultats séparés. Aucun test en jeu exécuté par l'assistant et aucune
publication effectuée.

## Préparation de la version 1.1.0

Le 27 novembre 2026, préparation demandée après validation utilisateur.
Numéro 1.1.0 ajouté au projet C#, au champ `modVersion` et à la description
Workshop d'About.xml, ainsi qu'au README et au changelog. Aucun changement
de logique de jeu depuis les tests utilisateur ; le binaire 1.7 a été
recompilé pour porter la nouvelle version. Le binaire 1.6 reste identique
à celui du tag 1.0.0.

Recompilation Release : 0 erreur, 0 avertissement. Contrôles de cohérence
réussis, y compris les versions des métadonnées ; ZIP de publication
contenant 16 fichiers vérifiés individuellement par SHA-256 et accompagné
de son empreinte. La procédure est dans `RELEASING.md`. Préparation locale
uniquement : aucun envoi GitHub ni publication Workshop.

La copie locale dans Mods a aussi été actualisée en 1.1.0, jeu fermé,
avec 13 fichiers vérifiés. La version de test précédente est conservée dans
`D:/SteamLibrary/steamapps/common/RimWorld/WPS-test-backups/20261127-151703-067310cf`.

## Publication GitHub et copie Workshop prête

Le 27 novembre 2026, sur demande explicite : branche `main` et tag `1.1.0`
poussés, références distantes vérifiées sur le commit de version `185f194`.
Release GitHub 1.1.0 publiée avec le ZIP et son fichier SHA-256 ; tailles,
état des fichiers et empreintes SHA-256 retournées par GitHub vérifiés.
La copie locale existante dans Mods a été comparée au ZIP publié :
13 fichiers runtime identiques, `modVersion=1.1.0`, packageId inchangé,
identifiant Workshop `3709575150` confirmé, aucun fichier parasite.
Aucune nouvelle copie nécessaire et aucun envoi Workshop effectué :
l'utilisateur publiera cette copie depuis le jeu.
Lien et empreinte du paquet consignés dans `RELEASING.md`.

## Correctif de l'aperçu Workshop

Le 27 novembre 2026, l'utilisateur rapporte « limit exceed » lors de l'envoi
Workshop. Aperçu initial de 1 819 921 octets, cause plausible mais pas
confirmée par un journal d'envoi. Image réduite à 960 × 640 et 971 059 octets,
inspectée visuellement, original conservé hors des fichiers runtime.
Contrôle bloquant sous 1 000 000 octets ajouté au script de cohérence.
Les deux skills RimWorld installés ont aussi reçu les consignes de taille,
de lisibilité et de vérification des copies déployées et empaquetées.

Après fermeture du jeu confirmée par l'utilisateur, seul `About/Preview.png`
a été remplacé dans Mods ; SHA-256 vérifié contre le dépôt. Les DLL validées
ont été conservées à l'identique et le ZIP a été reconditionné avec `-NoBuild` :
16 fichiers vérifiés, contrôles de cohérence réussis. Release GitHub 1.1.0
actualisée sans déplacer le tag. Nouvel essai Workshop à confirmer par
l'utilisateur ; en cas de persistance, relever l'erreur exacte et vérifier
le quota Steam Cloud.

## Tests en jeu obligatoires

1. Démarrer en 1.7 avec Harmony, Odyssey et WPS. Avec Biotech, sans HAR,
   vérifier les deux stands et les commandes WPS, sans erreur rouge.
2. Configurer un stand manuellement sans utiliser WPS : un colon doit y
   apporter une tenue. Appliquer puis effacer une règle WPS et répéter.
3. Avec HAR, sélectionner humain puis une race alien : le gizmo doit avoir
   une icône visible ; corps et tête doivent rester sélectionnables.
4. Avec HAR et Biotech, choisir le corps enfant. Dans Stockage, autoriser
   un vêtement enfant. Vérifier sa présence dans le filtre et son transport.
   Appliquer une règle autorisant ce vêtement : vérifier filtre et transport.
   Revenir à un corps adulte : les vêtements exclusivement enfant sont exclus.
5. Vérifier qualité, durabilité et permissions spéciales copiées depuis une
   règle. Une exclusion explicite de la règle doit rester exclue.
6. Charger une ancienne sauvegarde avec un stand WPS et un stand manuel.
   Vérifier les règles, sauvegarder, quitter et recharger ; recommencer le
   transport de vêtements adulte et enfant et vérifier l'absence d'erreurs.

## Régression facultative

- Sélection d'un corps enfant sur un stand contenant déjà une tenue adulte.
- Plusieurs stands avec règles différentes ; race alien puis humain.
- Chargement 1.6 : vérifier qu'une seule DLL WPS est chargée et que les
  fonctionnalités précédentes restent disponibles.

Le stand enfant masqué par HAR est intentionnel ; cette mise à jour conserve
ce choix de HAR et répare le stand commun.
