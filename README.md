# Bruktbørsen – ASP.NET Core Web API

Bruktbørsen er et REST-API utviklet i ASP.NET Core Web API (.NET 10) som del av **Porteføljeoppgave del 1 – Webapplikasjon 2**.

Applikasjonen fungerer som backend for en markedsplass for brukte varer. Brukere kan opprette annonser, gi bud, akseptere eller avslå bud, administrere favoritter og hente markedsstatistikk.

API-et er utviklet for å kunne brukes av en Blazor-klient i porteføljeoppgavens del 2.

## Teknologi

- .NET 10 / ASP.NET Core Web API
- ASP.NET Core Identity med roller
- Entity Framework Core
- SQLite
- Controllers, Services, Repositories og DTO-er
- Dependency Injection
- Microsoft OpenAPI og Scalar API Reference
- xUnit for automatiserte tester

## Forutsetninger

Installer følgende før du starter prosjektet:

- .NET 10 SDK
- Git
- EF Core CLI (`dotnet-ef`)

Kontroller installasjonen:

```powershell
dotnet --version
dotnet ef --version
```

Hvis `dotnet-ef` ikke er installert:

```powershell
dotnet tool install --global dotnet-ef
```

Prosjektet bruker EF Core 10. Bruk en kompatibel 10.x-versjon av EF Core CLI.

## 1. Klon prosjektet

```powershell
git clone https://github.com/Horites/Bruktborsen.git

cd Bruktborsen
```

## 2. Gjenopprett NuGet-pakker

```powershell
dotnet restore
```

## 3. Opprett databasen

Prosjektet bruker SQLite med tilkoblingsstrengen:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Data Source=bruktborsen.db"
    }
}
```

SQLite-databasen er ikke inkludert i Git-repositoriet.

Gå til API-prosjektets katalog:

```powershell
cd src/Bruktborsen.Api
```

Kjør eksisterende EF Core-migrasjoner:

```powershell
dotnet ef database update
```

Dette oppretter databasen `bruktborsen.db` i API-katalogen og anvender migrasjonene.

## 4. Start API-et

Fra `src/Bruktborsen.Api`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"

dotnet run --no-launch-profile --urls http://localhost:5000
```

API-et er nå tilgjengelig på:

`http://localhost:5000`

**Viktig:** Start fra API-katalogen, slik at den relative SQLite-tilkoblingsstrengen peker til riktig databasefil.

### Demo-data

I Development kjøres seeding ved oppstart. Systemet oppretter blant annet:

- Rollene `Admin` og `User`
- Fire demobrukere
- Fem kategorier
- Minst 50 demonstrasjonsannonser, hvorav minst 22 er solgte
- Bud med statusene `Pending`, `Accepted`, `Rejected` og `Withdrawn`
- Favoritter og prishistorikk

Seeding er utformet for å unngå duplikater ved gjentatt oppstart.

Demo-data opprettes ikke automatisk utenfor Development-miljøet.

## 5. Demobrukere

Alle demobrukerne har passordet `BruktDemo123!`.

| E-post           | Rolle | Navn          |
| ---------------- | ----- | ------------- |
| `admin@brukt.no` | Admin | Administrator |
| `ola@brukt.no`   | User  | Ola Nordmann  |
| `kari@brukt.no`  | User  | Kari Nordmann |
| `per@brukt.no`   | User  | Per Hansen    |

Disse kontoene er kun beregnet for lokal utvikling og demonstrasjon.

## 6. API-dokumentasjon

Når API-et kjører i Development, er interaktiv dokumentasjon tilgjengelig:

**Scalar API Reference**

`http://localhost:5000/scalar`

**OpenAPI JSON**

`http://localhost:5000/openapi/v1.json`

Scalar viser tilgjengelige endepunkter, forespørselsmodeller og responskoder.

## 7. Autentisering

API-et bruker ASP.NET Core Identity med bearer-tokens.

Registrering:

`POST /api/auth/register`

Innlogging:

`POST /api/auth/login`

Eksempel på innlogging med PowerShell:

```powershell
$body = @{
    email = "ola@brukt.no"
    password = "BruktDemo123!"
} | ConvertTo-Json

$login = Invoke-RestMethod `
    -Uri "http://localhost:5000/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

$headers = @{
    Authorization = "Bearer $($login.accessToken)"
}
```

Bruk `$headers` når du kaller beskyttede endepunkter.

Registrering oppretter vanlige brukere med rollen `User`. Administrative funksjoner krever rollen `Admin`.

Bearer-tokenet kommer fra ASP.NET Core Identity. Implementasjonen bruker ikke egendefinerte JWT-tokens.

## 8. API-endepunkter

### Autentisering

| Metode | Endepunkt            | Tilgang   |
| ------ | -------------------- | --------- |
| POST   | `/api/auth/register` | Alle      |
| POST   | `/api/auth/login`    | Alle      |
| GET    | `/api/auth/me`       | Innlogget |

### Kategorier

| Metode | Endepunkt              | Tilgang |
| ------ | ---------------------- | ------- |
| GET    | `/api/categories`      | Alle    |
| GET    | `/api/categories/{id}` | Alle    |
| POST   | `/api/categories`      | Admin   |
| PUT    | `/api/categories/{id}` | Admin   |
| DELETE | `/api/categories/{id}` | Admin   |

### Annonser

| Metode | Endepunkt            | Tilgang      |
| ------ | -------------------- | ------------ |
| GET    | `/api/listings`      | Alle         |
| GET    | `/api/listings/{id}` | Alle         |
| GET    | `/api/listings/my`   | Innlogget    |
| POST   | `/api/listings`      | Innlogget    |
| PUT    | `/api/listings/{id}` | Selger/Admin |
| DELETE | `/api/listings/{id}` | Selger/Admin |

Annonselisten støtter søk, kategori, tilstand, prisintervall og sortering.

Sortering kan settes til `newest`, `price_asc` eller `price_desc`.

### Bud

| Metode | Endepunkt                   | Tilgang          |
| ------ | --------------------------- | ---------------- |
| GET    | `/api/listings/{id}/offers` | Selger           |
| POST   | `/api/listings/{id}/offers` | Innlogget kjøper |
| GET    | `/api/offers/my`            | Innlogget        |
| POST   | `/api/offers/{id}/accept`   | Selger           |
| POST   | `/api/offers/{id}/reject`   | Selger           |
| POST   | `/api/offers/{id}/withdraw` | Budgiver         |

### Favoritter

| Metode | Endepunkt                    | Tilgang   |
| ------ | ---------------------------- | --------- |
| GET    | `/api/favorites`             | Innlogget |
| POST   | `/api/favorites/{listingId}` | Innlogget |
| DELETE | `/api/favorites/{listingId}` | Innlogget |

### Analyse

| Metode | Endepunkt                                                     | Tilgang   |
| ------ | ------------------------------------------------------------- | --------- |
| GET    | `/api/analytics/price-suggestion?categoryId=1&condition=Good` | Innlogget |
| GET    | `/api/analytics/categories/{id}/stats`                        | Alle      |
| GET    | `/api/analytics/overview`                                     | Alle      |
| GET    | `/api/analytics/listings/{id}/price-history`                  | Alle      |
| GET    | `/api/analytics/listings/{id}/price-assessment`               | Alle      |

Prisvurderingen returneres også som del av `GET /api/listings/{id}`.

## 9. Forretningsregler

API-et håndhever blant annet følgende regler:

1. Brukere kan ikke gi bud på egne annonser.
2. Bud kan bare opprettes på aktive annonser.
3. En kjøper kan bare ha ett ventende bud per annonse.
4. Et bud må være minst 50 % av annonseprisen.
5. Bare selgeren kan akseptere eller avslå bud.
6. Ved aksept markeres annonsen som solgt, og andre ventende bud avvises i samme databasetransaksjon.
7. Budgivere kan trekke egne ventende bud.
8. Prisendringer registreres automatisk.
9. Solgte annonser kan ikke endres.
10. Bare selger eller administrator kan endre eller fjerne annonser. Fjerning er en logisk sletting.

API-et bruker relevante HTTP-statuskoder, blant annet `400`, `401`, `403`, `404` og `409` ved feil eller konflikter.

## 10. Analysemodulen

Analysemodulen bruker historiske salg fra samme kategori.

Salgsprisene normaliseres med tilstandsfaktorer:

| Tilstand | Faktor |
| -------- | -----: |
| New      |   1,00 |
| LikeNew  |   0,85 |
| Good     |   0,70 |
| Used     |   0,55 |
| Defect   |   0,30 |

Normalisert salgspris:

```text
Normalisert pris = SoldPrice / Tilstandsfaktor
```

Prisforslag:

```text
Prisforslag =
    Median(normaliserte salgspriser)
    × ønsket tilstandsfaktor
```

Resultatet avrundes til nærmeste 10 kr. Minst tre solgte annonser kreves.

Prisvurderingen bruker kvartiler og interkvartilbredde (IQR):

```text
Nedre grense = Q1 - 1,5 × IQR
Øvre grense = Q3 + 1,5 × IQR
```

Priser utenfor grensene vurderes som `Low` eller `High`. Øvrige priser vurderes som `Normal`. Ved færre enn fem historiske salg returneres `Unknown`.

Kategoristatistikk omfatter gjennomsnittlig og median salgspris, utvalgsstandardavvik, salgstid, rabatt og salgsandel.

## 11. Prosjektstruktur

```text
Bruktborsen/
├── src/
│   └── Bruktborsen.Api/
│       ├── Controllers/
│       ├── DTOs/
│       ├── Data/
│       ├── Migrations/
│       ├── Models/
│       ├── Repository/
│       │   └── Interfaces/
│       ├── Services/
│       │   └── Interfaces/
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── Bruktborsen.Tests/
│       ├── Analytics/
│       ├── BusinessRules/
│       └── Controllers/
├── Bruktborsen.slnx
└── README.md
```

Applikasjonen følger lagdelingen:

```text
HTTP Request
    ↓
Controller
    ↓
Service
    ↓
Repository
    ↓
EF Core / SQLite
```

DTO-er brukes for data som sendes til og fra API-et.

## 12. Testing

Testprosjektet bruker xUnit og inkluderer tester for:

- Bud på egen annonse
- Bud på solgt annonse
- Duplikate ventende bud
- Minimumsbeløp på 50 %
- Budaksept og automatisk avslag
- Prisendringer
- Endring av solgte annonser
- Median, kvartiler, standardavvik og prisnormalisering
- Prisforslag og prisvurdering
- Kategoristatistikk og rabattberegning
- HTTP-statuskoder fra controllere
- Budaksept med foreldede data fra separate DbContext-instans­er

Kjør alle tester fra prosjektroten:

```powershell
dotnet test
```

Eller bygg og test i Release:

```powershell
dotnet build -c Release
dotnet test -c Release
```

Siste verifiserte resultat: **41 beståtte tester, 0 feil**.

## 13. Kjente begrensninger

- Analysemetoden forutsetter at varer innen samme kategori er tilstrekkelig sammenlignbare.
- Få historiske salg kan gi usikre prisforslag.
- Gamle salg kan være mindre representative for dagens prisnivå.
- Tilstandsfaktorene er faste og tar ikke hensyn til alle forskjeller mellom produkter.
- SQLite kan gi låsekonflikter ved samtidige skriveoperasjoner.
- Budaksept er beskyttet mot foreldede oppdateringer, men alle samtidige operasjoner er ikke stresstestet.
- Demo-seeding og API-dokumentasjon er begrenset til Development-miljøet.

## 14. Repository

GitHub:

<https://github.com/Horites/Bruktborsen>

Prosjektet er utviklet som del av porteføljeoppgaven i Webapplikasjon 2.
