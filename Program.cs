using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── OpenAPI / Scalar ───────────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, _) =>
    {
        document.Info.Title = "Hopital API";
        document.Info.Version = "v1";
        document.Info.Description = "API REST de la Clinique Saint-Lucas — Workshop WKS 2026";
        return Task.CompletedTask;
    });
});

// ── CORS — autoriser toutes les origines (workshop) ────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── JWT Authentication ─────────────────────────────────────
var jwtKey      = builder.Configuration["Jwt:Key"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── BUILD ──────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Clinique Saint-Lucas — API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// ════════════════════════════════════════════════════════════
// TEST — public (pas d'authentification requise)
// ════════════════════════════════════════════════════════════

app.MapGet("/test", () => Results.Ok(new
{
    success   = true,
    message   = "L'API de la Clinique Saint-Lucas fonctionne correctement.",
    timestamp = DateTime.UtcNow
}))
.WithName("Test")
.WithSummary("Test de connectivité")
.WithDescription("Endpoint de test — vérifie que l'API est bien démarrée et accessible.")
.WithTags("Test");

app.MapGet("/test/image", (HttpContext context) =>
    Results.Ok(new { imageUrl = "/images/SuccesImage.png" })
)
.WithName("TestImage")
.WithSummary("Test image")
.WithDescription("Retourne l'URL d'une image de test.")
.WithTags("Test");

// ════════════════════════════════════════════════════════════
// AUTH — public (pas d'authentification requise)
// ════════════════════════════════════════════════════════════

// Compte patient unique (hardcodé — pas de base de données)
var hardcodedPatient = new
{
    Id       = 1,
    Name     = "Marie Vandenberghe",
    Email    = "patient@clinique.be",
    Password = "Patient2026!",
    Role     = "patient"
};

app.MapPost("/api/patients/login", (LoginRequest req) =>
{
    if (req.Email != hardcodedPatient.Email || req.Password != hardcodedPatient.Password)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, hardcodedPatient.Id.ToString()),
        new Claim(ClaimTypes.Email,          hardcodedPatient.Email),
        new Claim(ClaimTypes.Name,           hardcodedPatient.Name),
        new Claim(ClaimTypes.Role,           hardcodedPatient.Role)
    };

    var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer:             jwtIssuer,
        audience:           jwtAudience,
        claims:             claims,
        expires:            DateTime.UtcNow.AddHours(24),
        signingCredentials: creds
    );

    return Results.Ok(new
    {
        token     = new JwtSecurityTokenHandler().WriteToken(token),
        tokenType = "Bearer",
        expiresIn = 86400,
        user      = new
        {
            id    = hardcodedPatient.Id,
            name  = hardcodedPatient.Name,
            email = hardcodedPatient.Email,
            role  = hardcodedPatient.Role
        }
    });
})
.WithName("Login")
.WithSummary("Connexion espace patient")
.WithDescription("Retourne un JWT Bearer token valide 24h. Credentials : patient@clinique.be / Patient2026!")
.WithTags("Auth");

app.MapPost("/api/patients/register", () =>
    Results.Json(
        new { message = "L'inscription en ligne n'est pas disponible. Veuillez contacter directement la clinique au +32 2 764 00 00." },
        statusCode: 501
    )
)
.WithName("Register")
.WithSummary("Inscription (non disponible)")
.WithTags("Auth");

// ════════════════════════════════════════════════════════════
// HOMEPAGE — protégé
// ════════════════════════════════════════════════════════════

app.MapGet("/api/homepage", () => Results.Ok(new
{
    hero = new
    {
        title        = "Votre santé, notre priorité",
        subtitle     = "La Clinique Saint-Lucas vous accueille 24h/24 avec des équipes médicales expertes et un équipement de pointe.",
        cta          = new { label = "Prendre rendez-vous", href = "/rendez-vous" },
        ctaSecondary = new { label = "Découvrir nos services", href = "/services" }
    },
    stats = new[]
    {
        new { label = "Lits disponibles",      value = "320",   icon = "bed"         },
        new { label = "Médecins spécialistes", value = "210",   icon = "stethoscope" },
        new { label = "Départements",          value = "18",    icon = "building"    },
        new { label = "Patients par mois",     value = "4 500", icon = "users"       }
    },
    sections = new[]
    {
        new { id = "services", title = "Nos services",  description = "Des soins spécialisés dans 18 domaines médicaux."  },
        new { id = "team",     title = "Notre équipe",  description = "210 médecins engagés pour votre santé."            },
        new { id = "news",     title = "Actualités",    description = "Les dernières nouvelles de la clinique."           }
    }
}))
.RequireAuthorization()
.WithName("Homepage")
.WithSummary("Contenu de la page d'accueil")
.WithTags("Homepage");

// ════════════════════════════════════════════════════════════
// SERVICES — protégé
// ════════════════════════════════════════════════════════════

var services = new[]
{
    new {
        id               = 1,
        slug             = "urgences",
        name             = "Urgences",
        shortDescription = "Service d'urgences ouvert 24h/24 et 7j/7.",
        description      = "Notre service des urgences accueille et prend en charge tous les patients nécessitant des soins immédiats. Une équipe pluridisciplinaire est disponible en permanence pour toute situation critique.",
        icon             = "ambulance",
        image            = "/images/services/urgences.jpg",
        phone            = "+32 2 764 11 11",
        hours            = "24h/24 — 7j/7",
        departmentId     = (int?)null
    },
    new {
        id               = 2,
        slug             = "cardiologie",
        name             = "Cardiologie",
        shortDescription = "Diagnostic et traitement des maladies cardiovasculaires.",
        description      = "Le service de cardiologie propose une prise en charge complète des pathologies cardiaques. Consultations, examens Holter, échocardiographie, cathétérisme cardiaque et chirurgie cardiovasculaire.",
        icon             = "heart",
        image            = "/images/services/cardiologie.jpg",
        phone            = "+32 2 764 22 22",
        hours            = "Lun–Ven : 8h00–18h00",
        departmentId     = (int?)1
    },
    new {
        id               = 3,
        slug             = "pediatrie",
        name             = "Pédiatrie",
        shortDescription = "Soins spécialisés pour les enfants de 0 à 16 ans.",
        description      = "Notre service de pédiatrie offre une prise en charge globale de l'enfant, de la naissance à l'adolescence. Consultations, hospitalisations, urgences pédiatriques et unité de soins intensifs néonatals (USIN).",
        icon             = "baby",
        image            = "/images/services/pediatrie.jpg",
        phone            = "+32 2 764 33 33",
        hours            = "Lun–Ven : 8h00–18h00",
        departmentId     = (int?)3
    },
    new {
        id               = 4,
        slug             = "radiologie",
        name             = "Radiologie & Imagerie",
        shortDescription = "Imagerie médicale haute définition : IRM, scanner, échographie.",
        description      = "Équipés des dernières technologies d'imagerie médicale, nos radiologues réalisent des examens précis pour des diagnostics fiables. IRM 3 Tesla, scanner multi-coupes, PET-scan, mammographie numérique.",
        icon             = "scan",
        image            = "/images/services/radiologie.jpg",
        phone            = "+32 2 764 44 44",
        hours            = "Lun–Sam : 7h30–19h00",
        departmentId     = (int?)null
    },
    new {
        id               = 5,
        slug             = "chirurgie",
        name             = "Chirurgie",
        shortDescription = "Chirurgie générale, viscérale et laparoscopique.",
        description      = "Le service de chirurgie intervient sur une large gamme de pathologies. Techniques mini-invasives (cœlioscopie, robot chirurgical Da Vinci), chirurgie ambulatoire et hospitalisations conventionnelles.",
        icon             = "scissors",
        image            = "/images/services/chirurgie.jpg",
        phone            = "+32 2 764 55 55",
        hours            = "Lun–Ven : 8h00–17h00",
        departmentId     = (int?)2
    },
    new {
        id               = 6,
        slug             = "maternite",
        name             = "Maternité",
        shortDescription = "Accompagnement de la grossesse jusqu'à l'accouchement et au-delà.",
        description      = "La maternité de la Clinique Saint-Lucas vous accompagne tout au long de votre grossesse dans un cadre chaleureux et sécurisé. Suivi prénatal, cours de préparation, accouchement physiologique ou médicalisé, et postnatal.",
        icon             = "baby-carriage",
        image            = "/images/services/maternite.jpg",
        phone            = "+32 2 764 66 66",
        hours            = "24h/24 — 7j/7",
        departmentId     = (int?)3
    }
};

app.MapGet("/api/services", () => Results.Ok(services))
.RequireAuthorization()
.WithName("GetServices")
.WithSummary("Liste des services médicaux")
.WithTags("Services");

app.MapGet("/api/services/{id:int}", (int id) =>
{
    var service = services.FirstOrDefault(s => s.id == id);
    return service is null
        ? Results.NotFound(new { message = $"Service {id} introuvable." })
        : Results.Ok(service);
})
.RequireAuthorization()
.WithName("GetService")
.WithSummary("Détail d'un service")
.WithTags("Services");

// ════════════════════════════════════════════════════════════
// DEPARTMENTS — protégé
// ════════════════════════════════════════════════════════════

var departments = new[]
{
    new {
        id          = 1,
        slug        = "cardiologie",
        name        = "Cardiologie & Cardiologie Interventionnelle",
        description = "Le département de cardiologie regroupe cardiologues cliniques et cardiologues interventionnels pour une prise en charge complète des maladies cardiovasculaires. Stroke unit coronarienne, laboratoire de cathétérisme et chirurgie cardiaque sur place.",
        head        = "Dr. Sophie Lecomte",
        floor       = "3ème étage — Aile B",
        phone       = "+32 2 764 20 00",
        teamCount   = 14,
        serviceId   = (int?)2
    },
    new {
        id          = 2,
        slug        = "chirurgie",
        name        = "Chirurgie Générale & Viscérale",
        description = "Notre département chirurgical dispose d'un plateau technique de pointe avec 8 blocs opératoires, un robot chirurgical Da Vinci et une unité de chirurgie ambulatoire de 20 places. Prise en charge digestive, hépatique, colorectale et bariatrique.",
        head        = "Dr. Anne-Claire Dubois",
        floor       = "2ème étage — Aile A",
        phone       = "+32 2 764 30 00",
        teamCount   = 18,
        serviceId   = (int?)5
    },
    new {
        id          = 3,
        slug        = "pediatrie",
        name        = "Pédiatrie & Néonatologie",
        description = "Dédié aux enfants de 0 à 16 ans, notre département pédiatrique comprend une unité de soins intensifs néonatals (USIN) de 12 berceaux, une unité pédiatrique de 28 lits, et des consultations spécialisées en allergologie, pneumologie et endocrinologie pédiatrique.",
        head        = "Dr. Isabelle Renard",
        floor       = "4ème étage — Aile C",
        phone       = "+32 2 764 40 00",
        teamCount   = 22,
        serviceId   = (int?)3
    },
    new {
        id          = 4,
        slug        = "neurologie",
        name        = "Neurologie",
        description = "Le département de neurologie prend en charge les pathologies du système nerveux central et périphérique. Stroke unit, consultation mémoire, épileptologie, neuro-oncologie et laboratoire d'EEG. Unité d'hospitalisation de 16 lits.",
        head        = "Dr. Nathalie Simon",
        floor       = "5ème étage — Aile B",
        phone       = "+32 2 764 50 00",
        teamCount   = 11,
        serviceId   = (int?)null
    }
};

app.MapGet("/api/departments", () => Results.Ok(departments))
.RequireAuthorization()
.WithName("GetDepartments")
.WithSummary("Liste des départements")
.WithTags("Departments");

app.MapGet("/api/departments/{id:int}", (int id) =>
{
    var dept = departments.FirstOrDefault(d => d.id == id);
    return dept is null
        ? Results.NotFound(new { message = $"Département {id} introuvable." })
        : Results.Ok(dept);
})
.RequireAuthorization()
.WithName("GetDepartment")
.WithSummary("Détail d'un département")
.WithTags("Departments");

// ════════════════════════════════════════════════════════════
// TEAM — protégé
// ════════════════════════════════════════════════════════════

var team = new[]
{
    new {
        id               = 1,
        departmentId     = 1,
        firstName        = "Sophie",
        lastName         = "Lecomte",
        title            = "Dr.",
        specialty        = "Cardiologue",
        bio              = "Cardiologue spécialisée en insuffisance cardiaque et cardiomyopathies. Chef du département de cardiologie depuis 2018. Formatrice à l'UCLouvain et membre du board de la Société Belge de Cardiologie.",
        languages        = new[] { "Français", "Anglais", "Néerlandais" },
        consultationDays = new[] { "Lundi", "Mercredi", "Vendredi" },
        phone            = "+32 2 764 21 01",
        email            = "s.lecomte@clinique-saintlucas.be",
        avatar           = "/images/team/lecomte.jpg"
    },
    new {
        id               = 2,
        departmentId     = 1,
        firstName        = "Marc",
        lastName         = "Vandenberghe",
        title            = "Dr.",
        specialty        = "Cardiologue Interventionnel",
        bio              = "Spécialiste en cathétérisme cardiaque, angioplastie coronarienne et pose de stents. 15 ans d'expérience en cardiologie interventionnelle. Formé à la Mayo Clinic (États-Unis).",
        languages        = new[] { "Français", "Néerlandais", "Anglais" },
        consultationDays = new[] { "Mardi", "Jeudi" },
        phone            = "+32 2 764 21 02",
        email            = "m.vandenberghe@clinique-saintlucas.be",
        avatar           = "/images/team/vandenberghe.jpg"
    },
    new {
        id               = 3,
        departmentId     = 2,
        firstName        = "Anne-Claire",
        lastName         = "Dubois",
        title            = "Dr.",
        specialty        = "Chirurgienne Générale",
        bio              = "Chef du département chirurgie. Spécialisée en chirurgie laparoscopique et robot-assistée. Pionnière de la chirurgie mini-invasive en Belgique, auteure de plus de 40 publications scientifiques.",
        languages        = new[] { "Français", "Anglais" },
        consultationDays = new[] { "Lundi", "Mardi", "Jeudi" },
        phone            = "+32 2 764 31 01",
        email            = "ac.dubois@clinique-saintlucas.be",
        avatar           = "/images/team/dubois.jpg"
    },
    new {
        id               = 4,
        departmentId     = 2,
        firstName        = "Pierre",
        lastName         = "Fontaine",
        title            = "Dr.",
        specialty        = "Chirurgien Viscéral",
        bio              = "Chirurgien viscéral et digestif spécialisé en chirurgie colorectale, hépatique et bariatrique (bypass gastrique, sleeve gastrectomie). Formé au CHU de Bordeaux.",
        languages        = new[] { "Français", "Anglais", "Espagnol" },
        consultationDays = new[] { "Mercredi", "Vendredi" },
        phone            = "+32 2 764 31 02",
        email            = "p.fontaine@clinique-saintlucas.be",
        avatar           = "/images/team/fontaine.jpg"
    },
    new {
        id               = 5,
        departmentId     = 3,
        firstName        = "Isabelle",
        lastName         = "Renard",
        title            = "Dr.",
        specialty        = "Pédiatre",
        bio              = "Chef du département pédiatrique. Pédiatre généraliste avec une expertise en maladies infectieuses de l'enfant et allergologie pédiatrique. Responsable du programme de vaccination infantile.",
        languages        = new[] { "Français", "Anglais" },
        consultationDays = new[] { "Lundi", "Mercredi", "Vendredi" },
        phone            = "+32 2 764 41 01",
        email            = "i.renard@clinique-saintlucas.be",
        avatar           = "/images/team/renard.jpg"
    },
    new {
        id               = 6,
        departmentId     = 3,
        firstName        = "Thomas",
        lastName         = "Laurent",
        title            = "Dr.",
        specialty        = "Néonatologue",
        bio              = "Néonatologue responsable de l'Unité de Soins Intensifs Néonatals (USIN). Spécialiste des prématurés et des pathologies néonatales complexes. Formé à l'Hôpital Necker (Paris).",
        languages        = new[] { "Français", "Anglais", "Allemand" },
        consultationDays = new[] { "Mardi", "Jeudi", "Samedi" },
        phone            = "+32 2 764 41 02",
        email            = "t.laurent@clinique-saintlucas.be",
        avatar           = "/images/team/laurent.jpg"
    },
    new {
        id               = 7,
        departmentId     = 4,
        firstName        = "Nathalie",
        lastName         = "Simon",
        title            = "Dr.",
        specialty        = "Neurologue",
        bio              = "Chef du département neurologique. Experte en accident vasculaire cérébral (AVC) et responsable de la Stroke Unit. Chercheuse associée à l'ULB, auteure de 60 publications sur la neuroprotection.",
        languages        = new[] { "Français", "Anglais" },
        consultationDays = new[] { "Lundi", "Jeudi" },
        phone            = "+32 2 764 51 01",
        email            = "n.simon@clinique-saintlucas.be",
        avatar           = "/images/team/simon.jpg"
    },
    new {
        id               = 8,
        departmentId     = 4,
        firstName        = "Jean-Pierre",
        lastName         = "Maes",
        title            = "Dr.",
        specialty        = "Neurologue — Épileptologue",
        bio              = "Spécialiste en épilepsie et troubles du sommeil. Responsable du laboratoire d'EEG et de la consultation mémoire. Coordinateur de la filière Alzheimer de la clinique.",
        languages        = new[] { "Français", "Néerlandais", "Anglais" },
        consultationDays = new[] { "Mardi", "Mercredi" },
        phone            = "+32 2 764 51 02",
        email            = "jp.maes@clinique-saintlucas.be",
        avatar           = "/images/team/maes.jpg"
    }
};

app.MapGet("/api/team", () => Results.Ok(team))
.RequireAuthorization()
.WithName("GetTeam")
.WithSummary("Liste de l'équipe médicale")
.WithTags("Team");

app.MapGet("/api/team/{id:int}", (int id) =>
{
    var doctor = team.FirstOrDefault(d => d.id == id);
    return doctor is null
        ? Results.NotFound(new { message = $"Médecin {id} introuvable." })
        : Results.Ok(doctor);
})
.RequireAuthorization()
.WithName("GetDoctor")
.WithSummary("Fiche d'un médecin")
.WithTags("Team");

// ════════════════════════════════════════════════════════════
// NEWS — protégé
// ════════════════════════════════════════════════════════════

var news = new[]
{
    new {
        id          = 1,
        title       = "Inauguration de notre nouvel IRM 3 Tesla",
        slug        = "inauguration-irm-3-tesla",
        summary     = "La Clinique Saint-Lucas investit dans un IRM dernière génération pour des diagnostics encore plus précis.",
        content     = "La Clinique Saint-Lucas est fière d'annoncer l'inauguration de son nouvel appareil d'IRM 3 Tesla, le plus puissant disponible actuellement pour un usage clinique. Cet équipement de pointe permet d'obtenir des images d'une précision inégalée, réduisant les temps d'examen et améliorant significativement le diagnostic. Il est particulièrement précieux en neurologie, cardiologie et oncologie. L'IRM 3T est opérationnel depuis le 1er mars 2026.",
        category    = "Équipement",
        author      = "Direction médicale",
        publishedAt = "2026-03-01",
        image       = "/images/news/irm.jpg",
        tags        = new[] { "IRM", "Technologie", "Neurologie" }
    },
    new {
        id          = 2,
        title       = "Campagne de vaccination printanière : inscrivez-vous",
        slug        = "campagne-vaccination-2026",
        summary     = "La clinique organise une campagne de vaccination ouverte à tous les patients jusqu'au 30 mars.",
        content     = "Dans le cadre de notre mission de santé publique, la Clinique Saint-Lucas organise du 15 au 30 mars 2026 une campagne de vaccination ouverte à tous. Vaccins disponibles : grippe saisonnière, pneumocoque, tétanos-polio et zona. Aucune ordonnance nécessaire. Inscription via le formulaire de contact ou par téléphone au +32 2 764 00 00.",
        category    = "Prévention",
        author      = "Dr. Isabelle Renard",
        publishedAt = "2026-02-20",
        image       = "/images/news/vaccination.jpg",
        tags        = new[] { "Vaccination", "Prévention", "Santé publique" }
    },
    new {
        id          = 3,
        title       = "Rénovation de la maternité : un espace repensé pour vous",
        slug        = "renovation-maternite-2026",
        summary     = "Notre service maternité a été entièrement rénové pour offrir un cadre plus chaleureux et moderne.",
        content     = "Après 18 mois de travaux, la maternité de la Clinique Saint-Lucas a rouvert ses portes dans un espace entièrement repensé. Chambres individuelles avec espace bébé intégré, salle de naissance naturelle et espace aquatique. Capacité portée à 28 lits de maternité et 12 berceaux USIN.",
        category    = "Infrastructure",
        author      = "Direction médicale",
        publishedAt = "2026-01-15",
        image       = "/images/news/maternite.jpg",
        tags        = new[] { "Maternité", "Rénovation", "Néonatologie" }
    },
    new {
        id          = 4,
        title       = "Prix national de la qualité des soins 2025",
        slug        = "prix-qualite-soins-2025",
        summary     = "La clinique récompensée par le Prix National de la Qualité des Soins pour son service de cardiologie.",
        content     = "Le service de cardiologie de la Clinique Saint-Lucas a reçu le Prix National de la Qualité des Soins 2025, décerné par le SPF Santé Publique. Ce prix reconnaît l'excellence du parcours patient, les faibles taux de réhospitalisation et l'innovation dans la prise en charge de l'insuffisance cardiaque. Félicitations au Dr. Lecomte et à toute son équipe.",
        category    = "Prix & Distinctions",
        author      = "Communication — Clinique Saint-Lucas",
        publishedAt = "2025-12-10",
        image       = "/images/news/prix.jpg",
        tags        = new[] { "Prix", "Cardiologie", "Qualité" }
    },
    new {
        id          = 5,
        title       = "Lancement du service de téléconsultation",
        slug        = "lancement-teleconsultation",
        summary     = "Consultez vos médecins en vidéo depuis chez vous, disponible dans toutes les spécialités.",
        content     = "La Clinique Saint-Lucas lance son service de téléconsultation disponible dans toutes les spécialités. Via le portail patient sécurisé, prenez rendez-vous en vidéo avec votre médecin. Ce service est remboursé par la mutuelle sous certaines conditions. Idéal pour les suivis post-opératoires, renouvellements d'ordonnances et consultations de contrôle.",
        category    = "Services",
        author      = "Direction numérique",
        publishedAt = "2025-11-05",
        image       = "/images/news/teleconsultation.jpg",
        tags        = new[] { "Téléconsultation", "Innovation", "Numérique" }
    }
};

app.MapGet("/api/news", () => Results.Ok(news))
.RequireAuthorization()
.WithName("GetNews")
.WithSummary("Liste des actualités")
.WithTags("News");

app.MapGet("/api/news/{id:int}", (int id) =>
{
    var article = news.FirstOrDefault(n => n.id == id);
    return article is null
        ? Results.NotFound(new { message = $"Article {id} introuvable." })
        : Results.Ok(article);
})
.RequireAuthorization()
.WithName("GetArticle")
.WithSummary("Détail d'un article")
.WithTags("News");

// ════════════════════════════════════════════════════════════
// FORMULAIRES POST — protégés
// ════════════════════════════════════════════════════════════

app.MapPost("/api/contact", (ContactRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.FirstName) ||
        string.IsNullOrWhiteSpace(req.Email)     ||
        string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { message = "Les champs prénom, email et message sont obligatoires." });
    }

    return Results.Ok(new
    {
        success     = true,
        message     = $"Merci {req.FirstName}, votre message a bien été envoyé. Notre équipe vous répondra dans les 24h ouvrables.",
        referenceId = $"CONTACT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
    });
})
.RequireAuthorization()
.WithName("Contact")
.WithSummary("Formulaire de contact")
.WithTags("Formulaires");

app.MapPost("/api/appointments", (AppointmentRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.FirstName)     ||
        string.IsNullOrWhiteSpace(req.Email)          ||
        string.IsNullOrWhiteSpace(req.PreferredDate))
    {
        return Results.BadRequest(new { message = "Les champs prénom, email et date souhaitée sont obligatoires." });
    }

    return Results.Ok(new
    {
        success               = true,
        message               = $"Votre demande de rendez-vous a été enregistrée. Vous recevrez une confirmation par email à {req.Email}.",
        appointmentId         = $"RDV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}",
        status                = "pending",
        estimatedConfirmation = "Sous 48h ouvrables"
    });
})
.RequireAuthorization()
.WithName("CreateAppointment")
.WithSummary("Demande de rendez-vous")
.WithTags("Formulaires");

app.Run();

// ─── Types ────────────────────────────────────────────────────────────────────
record LoginRequest(string Email, string Password);
record ContactRequest(string FirstName, string LastName, string Email, string Subject, string Message);
record AppointmentRequest(
    string  FirstName,
    string  LastName,
    string  Email,
    string  Phone,
    int?    DoctorId,
    int?    ServiceId,
    string  PreferredDate,
    string? Reason
);
