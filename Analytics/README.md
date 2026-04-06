# Playcus Analytics
Analytics tracking and facade for analytics sdk in Unity projects.

## How use?
* Find in template project AnalyticsManager prefab.
* AnalyticsManager must be linked in service loader (or mark as dontDestroyOnLoad).
* Link to every platform config on AnalyticsManager all AnalyticsSystem realisations setuped in your project.
* Use `ServiceLocator.Get<IAnalyticsManager>()` instead of directly calling any analytics sdk

## How conect new Analytics system?
* Realise abstract class AnalyticsSystem and connect realisation to AnalyticsManager prefab