# Note operative per le prossime sessioni Codex

Ultimo aggiornamento: 7 settembre 2026.

## Regole di lavoro

- Non usare lo stato Git come fonte attendibile: il repository non è sincronizzato.
- Non modificare `bin` e `obj`.
- L’utente prova normalmente l’applicazione da Visual Studio.
- Per verificare senza interferire con l’eseguibile aperto:

  `dotnet build --no-restore -o "$env:TEMP\SharePointToolbox-buildcheck"`

- Mantenere ReaLTaiizor; MaterialSkin non è più la UI applicativa.
- Colori, branding e testi configurabili devono restare centralizzati.

## Terminologia approvata

- Usare sempre “condivisione”, non “pratica”, nei testi visibili.
- “Autorizzazioni di accesso”.
- “Dettagli condivisione”.
- “Nuova condivisione” e “Apri esistente”.

## Palette corrente

- Blu istituzionale: `#3E5885`.
- Oro istituzionale: `#C9B76A`.
- Colore azioni critiche: `#B44B79`.
- Non modificare i colori senza richiesta esplicita.
- La fascia superiore usa il blu istituzionale, con filetto oro da 4 pixel.

## Dashboard attuale

File principale: `UI/MainForm.cs`.

- Finestra non ridimensionabile.
- Dimensione corrente compatta e non ridimensionabile, definita in `UI/MainForm.cs`.
- Barra comandi compatta nella parte superiore.
- Il logo è stato rimosso dalla Data Room perché ridondante rispetto al titolo.
- Area centrale unica, rettangolare, con:
  - autorizzazioni a sinistra;
  - dettagli della condivisione a destra;
  - separatore verticale;
  - una sola ombra complessiva;
  - intestazioni leggermente evidenziate.
- Barra di stato su due righe.
- Azioni autorizzazioni disposte su due righe.
- “Invia link” e “Revoca accessi” disposti verticalmente.

## Pulsanti dashboard

- Non usare `MaterialButton` o il `Button` nativo per questi comandi.
- È presente il controllo personalizzato `DashboardActionButton`.
- Larghezza dinamica calcolata da testo e icona.
- Altezza: 34 pixel.
- Testo in formato normale, su una riga.
- Icone lineari da 18 pixel.
- Colori ricavati dalla palette esistente.
- Pulsanti ordinari: fondo `Theme:DashboardButtonBackground` (`#E2E2E2`) e
  contenuto `Theme:DashboardButtonForeground` (`#212121`).
- Le azioni critiche restano nel colore `Danger`, con testo e icone bianchi.
- Il controllo deriva direttamente da `Control` per evitare le cornici native nere.
- Gli angoli trasparenti devono essere ripuliti usando il primo colore di sfondo opaco degli antenati (`ResolveOpaqueBackground`).

## Aspetto e controlli consolidati

- Il fondo degli elenchi resta bianco anche dopo apertura e chiusura dei popup.
- La selezione negli elenchi usa il blu istituzionale.
- Gli utenti esterni sono indicati con un pallino arancione.
- Le finestre usano controlli di minimizzazione, chiusura e ritorno alla home
  ricavati dalle strip grafiche presenti in `Resources`.
- Tutte le finestre derivate da `InstitutionalMaterialForm` ricevono l'icona
  applicativa centralmente.
- L'icona applicativa corrente deriva da `Resources/AppIcon.png` ed è disponibile
  anche come ICO multirisoluzione in `Resources/AppIcon.ico`.
- I messaggi applicativi utilizzano le icone istituzionali di
  `Resources/AlertIcons.png`.

## Caricamento iniziale

- La barra di avanzamento resta visibile finché l’app non è realmente pronta.
- Il controllo delle scadenze viene eseguito durante la fase progressiva.
- I metadati delle condivisioni vengono letti in parallelo, con massimo 6
  richieste simultanee.
- La cache dei nomi delle colonne SharePoint è protetta da `SemaphoreSlim`, per
  evitare richieste duplicate alla prima apertura.

## Scelte funzionali consolidate

- Gli owner non sono mostrati.
- Nell’elenco compaiono solo identità con email valida.
- Gli utenti esterni sono riconoscibili visivamente.
- La revoca automatica per scadenza rimuove soltanto gli accessi esterni.
- Le condivisioni non possono essere cancellate dall’applicazione.
- Audit applicativo salvato su Microsoft 365.
- Registro locale e audit visibili agli utenti autorizzati.
- Controllo del gruppo utenti applicativi eseguito prima di caricare la dashboard.
- Inviti e reinvio link usano la casella condivisa configurata.

## Configurazione Microsoft 365 nota

- Gruppo utilizzatori applicazione:
  `e58ce003-26ef-431e-ac9c-d98c3cc35a11`.
- Gruppo lettori audit:
  `70defbb8-181e-4bc0-8385-99a782a8195e`.
- Casella condivisa:
  `dataroom@giovanardilex.it`.
- ID oggetto casella:
  `56da3891-6323-42ca-8a5e-a661adaf2035`.

## Indicazione per la prossima sessione

Prima di analizzare nuovamente il progetto:

1. leggere questo file;
2. ispezionare soltanto i file coinvolti dalla nuova richiesta;
3. non ricostruire da zero la storia della migrazione grafica;
4. verificare sempre con compilazione in cartella temporanea.

## Document Hub e Rassegna Stampa

- Il nome complessivo del prodotto è ora `Document Hub`; `Data Room` rimane il
  nome del modulo delle condivisioni.
- Dopo autenticazione viene aperto `DocumentHubForm`, dal quale si sceglie
  l'ambiente Data Room o Rassegna Stampa.
- Il sito della rassegna è
  `https://giovanardieassociatistud.sharepoint.com/sites/RassegnaStampa`.
- La raccolta configurata è `Documenti condivisi`.
- Durante i test il destinatario della rassegna è configurabile; il mittente
  dedicato corrente è `rassegna_stampa@giovanardilex.it`.
- La struttura automatica è `anno / MM - Mese / dd-MM-yyyy`.
- Il modulo accetta più PDF tramite selezione file o area drag & drop.
- `Archivio rassegne` elenca le cartelle giornaliere e consente di riaprirle.
- I file esistenti sono caricati nella schermata come `Pubblicato`; aggiunte,
  sostituzioni e rimozioni restano differite fino al salvataggio.
- È possibile scaricare un documento selezionato da una rassegna archiviata.
- Il reinvio del collegamento propone un destinatario precompilato, modificabile
  con autocompletamento degli utenti interni e validazione dell'indirizzo.
- La pubblicazione e l'invio richiedono conferma preventiva.
- Per ogni cartella giornaliera viene creato un collegamento interno
  all'organizzazione in sola lettura; la mail usa quel collegamento e non il
  semplice `webUrl`, perché gli utenti non sono membri del sito.
- La rassegna riutilizza esattamente `Resources/EmailInvitationTemplate.html`;
  il servizio sostituisce soltanto etichetta, data, pulsante ed elenco PDF.
- Le pubblicazioni riuscite o fallite vengono registrate nell'audit
  applicativo centralizzato.

## Avvio e navigazione

- `ShapedStartupSplashForm` rimane visibile durante autorizzazione e preparazione
  iniziale, usando `Resources/StartupSplash.png`.
- Dopo il caricamento viene mostrato il `DocumentHubForm`, con scelta tra Data
  Room e Rassegna Stampa.
- Il ritorno alla home non deve ripetere il caricamento già completato.
- La chiusura delle finestre dei due moduli torna al Document Hub; la chiusura
  del Document Hub richiede conferma di uscita.
- Il controllo delle condivisioni scadute viene proposto entrando nella Data
  Room, non alla fine del caricamento generale.

## Funzioni Data Room consolidate dopo luglio 2026

- Archivio delle condivisioni attive con ricerca, evidenziazione hover, scheda
  informativa e aggiornamento manuale dell'elenco.
- Menu contestuale e doppio clic sulle autorizzazioni per modificare il ruolo.
- Conferma prima della rimozione di utenti e prima dell'invio degli inviti.
- Aggiunta multipla di destinatari, ruolo per riga e autocompletamento della
  directory interna anche per cognome.
- Modifica di nome, richiedente, note e scadenza della condivisione.
- Reinvio del collegamento ai partecipanti selezionati.
- Copia negli appunti del collegamento della condivisione selezionata.
- Notifica silente al supporto alla creazione di una nuova condivisione.
- Le attività SharePoint mostrate nell'audit derivano dalle informazioni rapide
  disponibili per la libreria; l'integrazione lenta con le ricerche Purview è
  stata abbandonata a favore dell'uso amministrativo dal portale.

## Branding e configurazione

- Font applicativo: Calibri, centralizzato tramite il tema.
- Colori, testi email, domini, indirizzi di supporto e branding sono configurati
  in `appsettings.json` e nelle relative classi `Options`.
- Il mittente Data Room corrente è `dataroom@giovanardilex.it`.
- Il mittente Rassegna Stampa corrente è
  `rassegna_stampa@giovanardilex.it`.
- Il dominio futuro previsto per il rebranding è `gbnlex.it`; il cambio non è
  ancora stato applicato.

## Funzioni discusse ma non implementate

- Non è stato implementato alcun sistema di esportazione o archiviazione delle
  mailbox Exchange/Outlook.
- Le valutazioni su PST, Outlook, Graph e Purview successive al 30 luglio 2026
  sono analisi progettuali e non modifiche al prodotto.

## Baseline di pubblicazione

- Ultima modifica operativa precedente alla presente messa in sicurezza:
  30 luglio 2026, aggiornamento e centralizzazione dell'icona applicativa.
- Il 7 settembre 2026 vengono aggiornate queste note e predisposta la prima
  baseline Git completa del lavoro accumulato dopo il 9 luglio 2026.
- Prima della pubblicazione verificare compilazione, assenza di segreti e corretta
  esclusione di `bin`, `obj`, `tmp` e file temporanei.
