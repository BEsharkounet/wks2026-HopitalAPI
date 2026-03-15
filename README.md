# Clinique Saint-Lucas — API

API REST de démonstration développée dans le cadre du **Workshop WKS 2026** (IFAPME).

Elle simule l'API d'un hôpital fictif — la **Clinique Saint-Lucas** — et permet aux étudiants de travailler avec de vraies données dynamiques sans avoir à développer un back-end eux-mêmes.

---

## Contexte pédagogique

Dans le cadre du workshop, les étudiants jouent le rôle d'une agence web mandatée par la Clinique Saint-Lucas pour refaire son site web. Cette API simule ce que fournirait le service informatique de l'hôpital dans un projet réel.

Les étudiants doivent :
- s'**authentifier** via `POST /api/patients/login` pour obtenir un Bearer token
- inclure ce token dans **toutes leurs requêtes** via le header `Authorization: Bearer <token>`
- consommer les endpoints `GET` pour afficher le contenu du site (services, équipe, actualités…)
- soumettre les formulaires via les endpoints `POST` (contact, rendez-vous)

---

## Stack technique

- **.NET 10** — Minimal API
- **Scalar** — Interface Swagger UI (`/scalar/v1`)
- **Microsoft.AspNetCore.OpenApi** — Génération du schéma OpenAPI
- **Microsoft.AspNetCore.Authentication.JwtBearer** — Authentification JWT
- **System.IdentityModel.Tokens.Jwt** — Génération des tokens

---

## Lancer l'API

```bash
dotnet run
```

| URL | Description |
|-----|-------------|
| `http://localhost:5150/scalar/v1` | Interface Swagger (Scalar UI) |
| `http://localhost:5150/openapi/v1.json` | Schéma OpenAPI JSON |
| `http://localhost:5150/test` | Endpoint de test de connectivité |

---

## Authentification

L'API utilise des **JWT Bearer tokens**. Tous les endpoints `/api/*` sont protégés — sans token valide, l'API retourne `401 Unauthorized`.

### Compte patient (hardcodé)

| Champ | Valeur |
|-------|--------|
| Email | `patient@clinique.be` |
| Mot de passe | `Patient2026!` |

### Obtenir un token

```http
POST /api/patients/login
Content-Type: application/json

{ "email": "patient@clinique.be", "password": "Patient2026!" }
```

**Réponse :**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "user": {
    "id": 1,
    "name": "Marie Vandenberghe",
    "email": "patient@clinique.be",
    "role": "patient"
  }
}
```

### Utiliser le token

Inclure dans chaque requête :
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Endpoints

### Test — publics (pas d'authentification requise)

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/test` | Test de connectivité |
| `GET` | `/test/image` | URL d'une image de test |

### Auth — publics

| Méthode | Route | Description | Body |
|---------|-------|-------------|------|
| `POST` | `/api/patients/login` | Connexion — retourne un JWT Bearer token | `{ email, password }` |
| `POST` | `/api/patients/register` | Non disponible — retourne 501 | — |

### Homepage — protégé 🔒

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/api/homepage` | Contenu de la page d'accueil (hero, stats, sections) |

**Réponse :** `hero` (title, subtitle, cta), `stats` (4 chiffres clés), `sections` (3 entrées)

### Services — protégés 🔒

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/api/services` | Liste des 6 services médicaux |
| `GET` | `/api/services/{id}` | Détail d'un service (id : 1 à 6) |

**Services disponibles :** Urgences (1), Cardiologie (2), Pédiatrie (3), Radiologie & Imagerie (4), Chirurgie (5), Maternité (6)

**Champs :** `id`, `slug`, `name`, `shortDescription`, `description`, `icon`, `image`, `phone`, `hours`, `departmentId`

### Départements — protégés 🔒

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/api/departments` | Liste des 4 départements |
| `GET` | `/api/departments/{id}` | Détail d'un département (id : 1 à 4) |

**Départements disponibles :** Cardiologie (1), Chirurgie (2), Pédiatrie & Néonatologie (3), Neurologie (4)

**Champs :** `id`, `slug`, `name`, `description`, `head`, `floor`, `phone`, `teamCount`, `serviceId`

### Équipe médicale — protégée 🔒

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/api/team` | Liste des 8 médecins |
| `GET` | `/api/team/{id}` | Fiche d'un médecin (id : 1 à 8) |

**Champs :** `id`, `departmentId`, `firstName`, `lastName`, `title`, `specialty`, `bio`, `languages`, `consultationDays`, `phone`, `email`, `avatar`

### Actualités — protégées 🔒

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/api/news` | Liste des 5 actualités |
| `GET` | `/api/news/{id}` | Détail d'un article (id : 1 à 5) |

**Champs :** `id`, `title`, `slug`, `summary`, `content`, `category`, `author`, `publishedAt`, `image`, `tags`

### Formulaires — protégés 🔒

| Méthode | Route | Description | Champs obligatoires |
|---------|-------|-------------|---------------------|
| `POST` | `/api/contact` | Formulaire de contact | `firstName`, `email`, `message` |
| `POST` | `/api/appointments` | Demande de rendez-vous | `firstName`, `email`, `preferredDate` |

**`POST /api/contact` — body :**
```json
{
  "firstName": "string",
  "lastName":  "string",
  "email":     "string",
  "subject":   "string",
  "message":   "string"
}
```

**`POST /api/appointments` — body :**
```json
{
  "firstName":     "string",
  "lastName":      "string",
  "email":         "string",
  "phone":         "string",
  "doctorId":      1,
  "serviceId":     2,
  "preferredDate": "2026-04-15",
  "reason":        "string"
}
```

---

## Codes de réponse

| Code | Signification |
|------|---------------|
| `200` | Succès |
| `400` | Données manquantes ou invalides |
| `401` | Token absent, invalide ou expiré |
| `404` | Ressource introuvable |
| `501` | Fonctionnalité non implémentée |

---

## CORS

L'API accepte les requêtes de **toutes les origines** (`AllowAnyOrigin`) — les fichiers HTML des étudiants peuvent l'appeler directement depuis `localhost` ou depuis un fichier local.

---

## Note

Toutes les données sont **fictives**. La Clinique Saint-Lucas n'existe pas. Cette API est exclusivement destinée à un usage pédagogique dans le cadre du WKS 2026.
