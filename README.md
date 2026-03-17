# Hivemind

Winnende inzending voor de infinibattle van 2026 (gearbots). Zie de bijgevoegde Guide.md voor een uitleg over het spel. 90% van de code is door A.I. geschreven.

## Strategie

- een leider, twee volgers.
- leider volgt vijanden, kiest targets verstuurt ze via de chat. bijvoorbeeld "CONTACT #2 @436,268 v7.50,7.54 s139". Dat is <id> <pos> <velocity> <step>
  - als een tankje niet meer gevonden kan worden zoeken we een nieuwe op basis van position staleness
- volgers richten op doelwit wat leider uitkiest, en doen zelf ook kennis op om beter te targeten.
- beweging is vrij simpel: we schuifelen gewoon heen en weer, vooruit en achteruit en draaien soms een klein beetje
- veel schieten en richten!

### bij het schieten wordt er rekening gehouden met
- waar de tegenstander gaat zijn op basis van waar hij voor het laatst gezien is
  - gecombineerd met hoe lang dat geleden is
  - gecombineerd met de snelheid
  - gecombineerd met frictie
  - gecombineerd met het feit dat je instant stopt als je tegen een muur aan komt
- dat  dan gecombineerd met hoe lang het duurt voor de kogel om daar te komen
- gecombineerd met de tijd die het duurt voor je turret om op dat mikpunt te komen
- en dan nog lettend op het niet raken van friendlies


## Waarom die strategie
In mijn zoektocht naar strategie zijn mij een paar dingen opgevallen.

- kogels ontwijken leek zinloos. het was beter om gewoon hard te rijden.
- bochten maken kost veel inputs, dus bij muren heb ik er voor gekozen om te reversen
- Ik heb ook nog replays gesanitized en aan A.I. gevoerd en daar kwamen verassend genoeg zinnige dingen uit. Dat zijn `replay-bullets.csv` en `replay-tanks.csv` Dat ging meer over finetuning van parameters dan echt nieuwe ideeën

## wat ik heb geleerd
- A.I. is best goed in dit soort dingen maken maar ik heb m best vaak een handje moeten helpen met het implementeren van iets
- de code is redelijk leesbaar maar best warrig
