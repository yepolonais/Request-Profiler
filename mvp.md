# Request Profiler
## Fonctionnalités principales
Voici les fonctionnalités exposées par Request Profiler :
- Temps de traitement : chronomètre demarré à l'entrée et à l'arrêt de la requête. Ajout du temps total dans les header de la réponse
- Logs des infos essentielles : Les informations essentielles sont loggés : 
    - méthode
    - url
    - code de retour
    - temps 
    - taille de la réponse
    Options pour logguer les headers ou le body
- Logguable dans n'importe quel ILogger: compatible avec ILogger de Microsoft
- Filtre : possibilité de filtrer certaines routes, comme celles de healthcheck
- Ajout d'un correlation Id auto
- Stockage temporaire des traces (in memory ou redis)
- visualiseur des dernières requêtes avec une UI Blazor
- Bonus : intégration Open Telemetry

## Architecture

RequestProfiler.sln
│
├── src/
│   ├── RequestProfiler.Middleware/         → Middleware réutilisable (core + extension)
│   ├── RequestProfiler.Storage/            → Abstraction et implémentations de stockage
│   ├── RequestProfiler.UI/                 → UI Blazor Server pour visualiser les requêtes
│   └── RequestProfiler.SampleApp/          → Web API de test pour démo locale
│
├── tests/
│   ├── RequestProfiler.Middleware.Tests/   → Tests unitaires du middleware
│   └── RequestProfiler.Storage.Tests/      → Tests des implémentations de stockage
│
└── build/                                  → Fichiers de pipeline, Dockerfile, etc.
