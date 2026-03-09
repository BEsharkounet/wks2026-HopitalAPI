# Clinique Saint-Lucas — API

API REST de démonstration développée dans le cadre du **Workshop WKS 2026** (IFAPME).

Elle simule l'API d'un hôpital fictif — la **Clinique Saint-Lucas** — et permet aux étudiants de travailler avec de vraies données dynamiques sans avoir à développer un back-end eux-mêmes.

---

## Contexte pédagogique

Dans le cadre du workshop, les étudiants jouent le rôle d'une agence web mandatée par la Clinique Saint-Lucas pour refaire son site web. Cette API simule ce que fournirait le service informatique de l'hôpital dans un projet réel.

Les étudiants doivent :
- consommer les endpoints `GET` pour afficher le contenu du site (services, équipe, actualités…)
- soumettre les formulaires via les endpoints `POST` (contact, rendez-vous, inscription patient)

---

## Stack technique

- **.NET 10** — Minimal API
- **Scalar** — Interface Swagger UI (`/scalar/v1`)
- **Microsoft.AspNetCore.OpenApi** — Génération du schéma OpenAPI

---

## Lancer l'API

```bash
dotnet run
```

| URL | Description |
|-----|-------------|
| `http://localhost:5000/scalar/v1` | Interface Swagger (Scalar UI) |
| `http://localhost:5000/openapi/v1.json` | Schéma OpenAPI JSON |
| `http://localhost:5000/test` | Endpoint de test de connectivité |

---

## Endpoints

### Disponibles

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/test` | Test de connectivité |

### A venir (par itération)

**Itération 2 — Lecture de données**

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/api/homepage` | Contenu de la page d'accueil |
| `GET` | `/api/services` | Liste des services médicaux |
| `GET` | `/api/services/{id}` | Détail d'un service |
| `GET` | `/api/departments` | Liste des départements |
| `GET` | `/api/departments/{id}` | Détail d'un département |
| `GET` | `/api/team` | Liste de l'équipe médicale |
| `GET` | `/api/team/{id}` | Fiche d'un médecin |
| `GET` | `/api/news` | Liste des actualités |
| `GET` | `/api/news/{id}` | Détail d'un article |

**Itération 3 — Soumission de formulaires**

| Méthode | Route | Description |
|---------|-------|-------------|
| `POST` | `/api/contact` | Formulaire de contact |
| `POST` | `/api/appointments` | Demande de rendez-vous |
| `POST` | `/api/patients/register` | Inscription espace patient |
| `POST` | `/api/patients/login` | Connexion espace patient |

---

## Note

Toutes les données sont **fictives**. La Clinique Saint-Lucas n'existe pas. Cette API est exclusivement destinée à un usage pédagogique dans le cadre du WKS 2026.
