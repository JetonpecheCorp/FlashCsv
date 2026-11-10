# FlashCsv 

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-100%25-brightgreen.svg)](#-pourquoi-choisir-flashcsv)
[![Zero Allocation](https://img.shields.io/badge/Memory-High%20Performance-orange.svg)](#-performances--benchmarks)

<p align="center">
  <img src="assets/logoFlashCsv.png" alt="FlashXlsx Logo" width="220" />
</p>

**FlashCsv** est une bibliothèque .NET ultra-performante dédiée au parsing, au streaming et à la sérialisation CSV. Conçue de A à Z avec les **Source Generators incrémentaux C#** (`IIncrementalGenerator`), elle garantit une compatibilité totale avec **Native AOT**, élimine toute réflexion au runtime et minimise les allocations mémoire grâce à une architecture bas-niveau (`ref struct`, `Span<byte>`, `ArrayPool<T>`).

## Sommaire

1. [Pourquoi choisir FlashCsv ?](#pourquoi-choisir-flashcsv-)
2. [Performances & Benchmarks](#performances--benchmarks)
3. [Installation](#installation)
4. [Démarrage rapide (Débutant)](#démarrage-rapide-débutant)
   * [1. Déclarer son premier modèle](#1-déclarer-son-premier-modèle)
   * [2. Lire une chaîne ou un fichier](#2-lire-une-chaîne-ou-un-fichier)
   * [3. Écrire une chaîne ou un fichier](#3-écrire-une-chaîne-ou-un-fichier)
5. [Gestion des erreurs et Résilience (Sans exception)](#-gestion-des-erreurs-et-résilience-sans-exception)
   * [Mode permissif (`CollectAndContinue`)](#1-mode-permissif-collectandcontinue--recommandé-pour-les-imports)
   * [Mode strict (`FailFast`)](#2-mode-strict-failfast)
6. [Schémas de fonctionnement & Ordre des options](#-schémas-de-fonctionnement)
   * [Cycle de désérialisation (Lecture)](#1-cycle-de-désérialisation-lecture)
   * [Cycle de sérialisation (Écriture)](#2-cycle-de-sérialisation-écriture)
7. [Guide exhaustif des Attributs](#guide-exhaustif-des-attributs)
   * [Structure & Colonnes](#structure--colonnes)
   * [Formats, Conversions & Collections](#formats-conversions--collections)
   * [Validation de données](#validation-de-données)
   * [Sécurité & Assainissement](#sécurité--assainissement)
8. [Exemples Pratiques du Quotidien](#exemples-pratiques-du-quotidien)
   * [Validation métier & Bornes](#1-validation-métier-et-bornes)
   * [Tableaux et listes dans une cellule (`[CsvDelimiterArray]`)](#2-listes-et-tableaux-dans-une-seule-cellule)
   * [Sous-objets et sous-sous-objets aplatis en détail (`[CsvFlatten]`)](#3-sous-objets-et-sous-sous-objets-aplatis-csvflatten)
   * [Mapping dynamique d'import (`ColumnMapping`)](#4-mapping-dynamique-dimport-columnmapping)
   * [Traduction dynamique d'en-têtes à l'écriture (`FormatHeader`)](#5-traduction-et-renommage-des-en-têtes-formatheader)
   * [Transformation globale à la volée](#6-transformation-globale-à-la-volée-transformfield)
9. [Cas d'usage Avancés & Spécialisés](#-cas-dusage-avancés--spécialisés)
   * [Insertion massive ultra-rapide en base (`SqlBulkCopy` / `CsvDataReader`)](#1-insertion-massive-en-base-de-données-sqlbulkcopy)
   * [Objets et listes d'objets en JSON AOT (`[CsvJson]`)](#2-objets-et-listes-dobjets-complexes-en-json-aot)
   * [Polymorphisme et types dérivés (`[CsvDiscriminator]`)](#3-polymorphisme-et-types-dérivés)
   * [Règles conditionnelles sans allocation (`ICsvCondition<T>`)](#4-règles-conditionnelles-sans-allocation-icsvconditiont)
   * [Convertisseur binaire simple et parlant (`ICsvFieldReader<T>`)](#5-convertisseur-binaire-simple-icsvfieldreadert)
10. [Astuces pour les Confirmés & Experts](#astuces-pour-les-confirmés--experts)
11. [Référence complète des Options](#️-référence-complète-des-options)
    * [`CsvReaderOptions`](#csvreaderoptions-lecture)
    * [`CsvWriterOptions`](#csvwriteroptions-écriture)
12. [Exemple ultime](#lexemple-ultime-all-in-one)
13. [Licence](#licence)

## Pourquoi choisir FlashCsv ?

La majorité des parseurs CSV traditionnels s'appuient sur la **réflexion C#** (`Type.GetProperties()`, `Activator.CreateInstance()`). Ces mécanismes induisent des allocations massives sur le tas (Heap), saturent le Ramasse-Miettes (GC) et provoquent des régressions ou des avertissements lors de la compilation **Native AOT**.

| Critère | Parseurs Traditionnels | FlashCsv ⚡ |
|:---|:---|:---|
| **Compilation Native AOT** | Incompatible ou avertissements de Trimming | **100% Native AOT & Trimming-Friendly** |
| **Génération du code** | Réflexion / Émission dynamique (`IL.Emit`) | **Source Generator Roslyn C#** à la compilation |
| **Gestion mémoire** | Multiples chaînes temporaires et boxing | **Mémoire maîtrisée** (`ref struct`, `Span<byte>`, `ArrayPool`) |
| **Gros volumes (Go)** | Risque de saturation mémoire (`OOM`) | **Streaming asynchrone pur** via `IAsyncEnumerable<T>` |
| **Sécurité intégrée** | Injections de formules Excel non filtrées | **Protection automatique** (`EscapeFormulas`, XSS) |
| **Alimentation Base SQL** | Nécessite des listes intermédiaires | **`DbDataReader` natif streaming** pour `SqlBulkCopy` |

## Performances & Benchmarks

Mesures officielles réalisées avec **BenchmarkDotNet v0.15.8** sur **Windows 11** (.NET SDK 10.0.403, RyuJIT x86-64-v4, Intel Core i5-1145G7 2.60GHz) :

### 1. Jeu de données : 100 000 lignes

| Opération | Bibliothèque | Temps moyen (Mean) | Erreur | Écart-type (StdDev) | Rang | Gen 0 | Gen 1 | Mémoire allouée |
|:---|:---|---:|---:|---:|:---:|---:|---:|---:|
| **Lecture (Read)** | **FlashCsv** ⚡ | **17.11 ms** | 0.126 ms | 0.112 ms | **2** | 937.5000 | - | **3.85 MB** |
| Lecture (Read) | Sylvan | 18.44 ms | 0.163 ms | 0.144 ms | 3 | 1000.0000 | 156.2500 | 4.04 MB |
| **Écriture (Write)** | **FlashCsv** ⚡ | **15.69 ms** | 0.298 ms | 0.278 ms | **1** | 750.0000 | - | **3.05 MB** |

### 2. Jeu de données : 1 000 000 lignes (1 Million)

| Opération | Bibliothèque | Temps moyen (Mean) | Erreur | Écart-type (StdDev) | Rang | Gen 0 | Mémoire allouée |
|:---|:---|---:|---:|---:|:---:|---:|---:|
| **Lecture (Read)** | **FlashCsv** ⚡ | **171.97 ms** | 3.135 ms | 2.779 ms | **2** | 9333.3333 | **38.16 MB** |
| Lecture (Read) | Sylvan | 193.12 ms | 2.230 ms | 1.862 ms | 3 | 10000.0000 | 40.00 MB |
| **Écriture (Write)** | **FlashCsv** ⚡ | **159.60 ms** | 2.696 ms | 2.390 ms | **1** | 7333.3333 | **30.52 MB** |

> **Résultats clés :**
> - **Lecture :** FlashCsv est **~11% à 12% plus rapide** que Sylvan tout en réduisant la mémoire allouée (aucune collection Gen 1 déclenchée).
> - **Écriture :** FlashCsv écrit **1 million de lignes en seulement ~159 ms** avec à peine 30 MB alloués grâce à son formatage direct UTF-8 sans boxing.

## Installation

```bash
dotnet add package FlashCsv
```

## Démarrage rapide (Débutant)

### 1. Déclarer son premier modèle

Décorez votre classe, struct ou record avec **`[CsvSerializable]`** et ajoutez le mot-clé **`partial`** :

```csharp
using FlashCsv.Annotations;

namespace MonApp;

[CsvSerializable]
public partial record struct Client
{
    [CsvName("id_client", "id")]
    public int Id { get; init; }

    [CsvName("nom")]
    public string Nom { get; init; }

    [CsvName("solde")]
    public double Solde { get; init; }
}
```

> 💡 **Pourquoi `partial` ?** FlashCsv écrit la seconde moitié de votre classe en arrière-plan lors de la compilation pour y implémenter `ICsvSerializable<Client>` sans aucune réflexion.

### 2. Lire une chaîne ou un fichier

#### En mémoire depuis une chaîne de caractères
```csharp
using FlashCsv.Core;

string csv = """
id_client,nom,solde
1,Alice,1250.50
2,Bob,3400.00
""";

List<Client> clients = FlashCsvSerializer.Deserialize<Client>(csv);

foreach (var c in clients)
{
    Console.WriteLine($"Client #{c.Id} : {c.Nom} ({c.Solde} €)");
}
```

#### En streaming asynchrone depuis un fichier (Gros volumes)
```csharp
using FlashCsv.Core;

await using var stream = File.OpenRead("clients_import.csv");

// Ne charge jamais tout le fichier en mémoire : traitement ligne par ligne !
await foreach (Client c in FlashCsvSerializer.DeserializeAsync<Client>(stream))
{
    Console.WriteLine($"Importé : {c.Nom}");
}
```

### 3. Écrire une chaîne ou un fichier

```csharp
using FlashCsv.Core;

var clients = new[]
{
    new Client { Id = 1, Nom = "Alice", Solde = 1250.50 },
    new Client { Id = 2, Nom = "Bob", Solde = 3400.00 }
};

// 1. Obtenir une chaîne de caractères
string texteCsv = FlashCsvSerializer.SerializeToString(clients);

// 2. Ou écrire directement dans un fichier
await using var outStream = File.Create("export_clients.csv");
await FlashCsvSerializer.SerializeAsync(outStream, clients);
```

## Gestion des erreurs et Résilience (Sans exception)

Dans FlashCsv, la gestion des erreurs ne nécessite **aucun bloc `try...catch`**. Les erreurs sont directement transmises à votre callback `OnError` sans interrompre votre code par une exception.

### 1. Mode permissif (`CollectAndContinue`) — Recommandé pour les imports

Ce mode ignore silencieusement la ligne défectueuse, appelle votre callback `OnError` avec les informations d'anomalie, puis continue de lire les lignes valides suivantes :

```csharp
using FlashCsv.Core;

string csvInvalide = """
id_client,nom,solde
1,Alice,1250.50
2,Bob,cent-euros
3,Charlie,890.00
""";

var options = new CsvReaderOptions
{
    ErrorMode = CsvErrorMode.CollectAndContinue,
    OnError = error =>
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Erreur Ligne {error.LineNumber}] " +
                          $"Colonne '{error.ColumnName}' : Code {error.Code}");
        Console.ResetColor();
    }
};

using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvInvalide));

// Seuls Alice (#1) et Charlie (#3) seront retournés
await foreach (var client in FlashCsvSerializer.DeserializeAsync<Client>(stream, options))
{
    Console.WriteLine($"Valide : #{client.Id} {client.Nom}");
}
```

**Sortie console :**
```text
Valide : #1 Alice
[Erreur Ligne 3] Colonne 'solde' : Code InvalidFloat
Valide : #3 Charlie
```

### 2. Mode strict (`FailFast`)

Si la moindre anomalie doit couper le flux, le mode `FailFast` avertit le gestionnaire d'erreur `OnError` puis arrête proprement l'itération sans lever d'exception :

```csharp
var options = new CsvReaderOptions
{
    ErrorMode = CsvErrorMode.FailFast, // Arrêt dès le premier problème
    OnError = error =>
    {
        Console.WriteLine($"[Arrêt d'urgence] Ligne {error.LineNumber} corrompue sur {error.ColumnName}.");
    }
};

await foreach (var client in FlashCsvSerializer.DeserializeAsync<Client>(stream, options))
{
    // Ne recevra aucune ligne après la ligne défectueuse
}
```

## 🔄 Schémas de fonctionnement

### 1. Cycle de désérialisation (Lecture)

```text
📥 FLUX CSV BRUT (Fichier, Mémoire, Réseau)
    │
    ▼
┌────────────────────────────────────────────────────────┐
│  PHASE 1 : TAMPON & FLUX (CsvStreamReader)             
│  Options : BufferSize • CommentPrefix • BlankLines     
│                                                        
│  • Emprunt d'un buffer zéro-alloc (ArrayPool<byte>)    
│  • Filtrage à la volée des lignes vides et dièses (#)  
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  PHASE 2 : FORMAT & EN-TÊTES (DelimiterSniffer)        
│  Options : Delimiter • CandidateDelimiters • HasHeader 
│                                                        
│  • Auto-détection du séparateur si Delimiter == null   
│  • Lecture de la ligne 0 et mapping des colonnes       
│  • Application de ColumnMapping (priorité sur CsvName) 
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  PHASE 3 : TOKENISATION ULTRA-RAPIDE (CsvRowTokenizer) 
│  Options : Quote • MaxFieldSize • IgnoreExtraColumns   
│                                                        
│  • Découpage sans copie via SearchValues<byte> (SIMD)  
│  • Gestion des guillemets doubles RFC 4180 ("")        
│  • Garde-fou mémoire : rejet si taille > MaxFieldSize  
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  PHASE 4 : NORMALISATION DE CELLULE (CsvRowView)       
│  Options : TrimWhitespace • NullValues • StringPooling 
│                                                        
│  • Élagage des espaces en bordure si activé           
│  • Détection immédiate des valeurs nulles ("NA", "\\N")
│  • Mise en cache CsvStringPool pour les textes répétés 
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  PHASE 5 : PARSING TYPÉ (Source Generator AOT)         
│  Options : AllowCommaDecimalSeparator • DateTimeFormat 
│                                                        
│  • Nombres : Utf8Parser direct (remplace ',' par '.')  
│  • Dates : Fast-path ISO 8601 en binaire UTF-8         
│  • Validations : [CsvRange], [CsvPattern], etc.        
└─────────────┬────────────────────────────┬─────────────┘
              │                            │
      [Succès du parsing]          [Erreur détectée]
              │                            │
              ▼                            ▼
      INSTANCE T              GESTION D'INCIDENT
      (Yield Return)              Options : ErrorMode • OnError
                                  • Déclenche le rappel OnError
                                  • CollectAndContinue ➔ saute ligne
                                  • FailFast ➔ arrêt propre
```

### 2. Cycle de sérialisation (Écriture)

```text
OBJETS MÉTIER EN MÉMOIRE (IEnumerable<T> / IAsyncEnumerable<T>)
    │
    ▼
┌────────────────────────────────────────────────────────┐
│  ÉTAPE 1 : LIGNE D'EN-TÊTE                            
│  Options : WriteHeader • FormatHeader • Delimiter      
│                                                        
│  • Si WriteHeader == true : génération des titres      
│  • [CsvName] fournit le nom officiel de chaque colonne 
│  • FormatHeader applique une traduction / renommage    
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  ÉTAPE 2 : FORMATAGE PRIMITIF (IUtf8SpanFormattable)  
│  Options : NullFormat • TransformField                 
│                                                        
│  • Nombres / Dates : écrit directement en UTF-8        
│  • JSON : sérialisé via Utf8JsonWriter sans allocation 
│  • Si valeur null : insertion du texte NullFormat      
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  ÉTAPE 3 : SÉCURITÉ ANTI-INJECTION                     
│  Options : EscapeFormulas • [CsvSanitizeFormula]       
│                                                        
│  • Détecte si la cellule débute par =, +, -, @ ou \t   
│  • Préfixe par une apostrophe (') pour Excel / Calc    
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  ÉTAPE 4 : ÉCHAPPEMENT & QUOTING RFC 4180              
│  Options : QuoteStrategy • Quote • Delimiter           
│                                                        
│  • Minimal : entoure de "" si délimiteur, guillemet, \n
│  • Always  : entoure systématiquement toute cellule    
│  • Dédoublement automatique des guillemets internes    
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  ÉTAPE 5 : VIDAGE SUR LE DISQUE / RÉSEAU               
│  Options : NewLine • BufferSize                        
│                                                        
│  • Concatène le délimiteur d'octet et le saut NewLine  
│  • Vidage automatique (Flush) du buffer ArrayPool      
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
 FICHIER CSV PRODUIT (UTF-8)
```

## Guide exhaustif des Attributs

### Structure & Colonnes

| Attribut | Cible | Rôle |
|:---|:---|:---|
| `[CsvSerializable]` | Classe / Struct / Record | Active le générateur incrémental Roslyn pour ce type. |
| `[CsvName("principal", "alias1", ...)]` | Propriété / Champ | Nom officiel d'écriture et liste d'alias acceptés en lecture. |
| `[CsvOrder(int)]` | Propriété / Champ | Ordre physique d'agencement des colonnes à l'écriture. |
| `[CsvRowNumber]` | Propriété (`int`, `long`) | Injecte automatiquement l'index de ligne source (1-based). |
| `[CsvIgnore]` | Propriété / Champ | Ignore la propriété à la lecture et à l'écriture. |
| `[CsvFlatten("prefix_")]` | Propriété / Champ | Projette les propriétés d'un sous-objet dans les colonnes du parent. |
| `[CsvExtensionData]` | Propriété / Champ | Stocke toutes les colonnes inconnues dans un `Dictionary<string, string>`. |

### Formats, Conversions & Collections

| Attribut | Cible | Rôle |
|:---|:---|:---|
| `[CsvFormat("...")]` | Propriété / Champ | Masque de formatage explicite (ex. `"dd/MM/yyyy"`, `"0.00"`). |
| `[CsvDelimiterArray(';')]` | Propriété / Champ | Mappe une collection/tableau dans une cellule avec un délimiteur interne. |
| `[CsvJson("colonne", ...)]` | Propriété / Champ | Mappe une cellule contenant du JSON direct compatible AOT. |
| `[CsvNullValues("NA", "\\N", ...)]` | Propriété / Champ | Définit les valeurs reconnues comme `null` sur cette colonne. |
| `[CsvBoolean([...], [...])]` | Propriété / Champ | Valeurs textuelles admises pour `true` et `false`. |
| `[CsvEnum(CsvEnumFormat)]` | Propriété / Champ | Sérialisation de l'énumération par libellé (`Name`) ou valeur (`UnderlyingValue`). |
| `[CsvEnumMemberName("Val", ...)]` | Membre d'enum | Nom officiel et alias de désérialisation pour une valeur d'enum. |
| `[CsvConverter<TConverter>]` | Propriété / Champ | Raccorde un lecteur/écrivain personnalisé sans réflexion. |

> L'attribut [CsvFormat("...")] est conçu pour s'appliquer à **tous les types temporels, numériques et aux identifiants** supportés par le moteur d'écriture et de lecture de FlashCsv :
`DateTime`, `DateOnly`, `TimeOnly`, `decimal`, `double`, `int`, `Guid`.  
En résumé, il fonctionne sur **tout type .NET implémentant** `ISpanFormattable` / `IFormattable`.

### Validation de données

| Attribut | Cible | Rôle |
|:---|:---|:---|
| `[CsvRequired]` | Propriété / Champ | La cellule ne doit être ni absente ni vide (`RequiredFieldMissing`). |
| `[CsvRequiredIf<TCondition>]` | Propriété / Champ | Obligation conditionnelle évaluée statiquement sans allocation. |
| `[CsvRange(min, max)]` | Propriété / Champ | Valide qu'un nombre est dans l'intervalle `[min, max]` (inclusif). |
| `[CsvMin(val)]` / `[CsvMax(val)]` | Propriété / Champ | Borne minimale ou maximale numérique. |
| `[CsvLength(min, max)]` | Propriété / Champ | Valide la longueur d'une chaîne entre deux bornes. |
| `[CsvMinLength(n)]` / `[CsvMaxLength(n)]` | Propriété / Champ | Longueur minimale ou maximale d'une chaîne. |
| `[CsvPattern(@"regex")]` | Propriété / Champ | Valide le champ contre une expression régulière. |
| `[CsvAllowedValues("A", "B", ...)]` | Propriété / Champ | Liste blanche stricte des valeurs textuelles acceptées. |

### Sécurité & Assainissement

| Attribut | Cible | Rôle |
|:---|:---|:---|
| `[CsvSanitizeFormula]` | Propriété / Champ | Ajoute un `'` si le champ débute par `=`, `+`, `-`, `@`, `\t` ou `\r`. |
| `[CsvSanitizeHtml(mode)]` | Propriété / Champ | Encode ou supprime les balises HTML (`StripTags` / `Encode`). |
| `[CsvCase(CsvCaseMode)]` | Propriété / Champ | Normalise la casse (`Upper`, `Lower`, `Title`). |

## Exemples Pratiques du Quotidien

### 1. Validation métier et Bornes

```csharp
using FlashCsv.Annotations;

[CsvSerializable]
public partial record struct ProfilUtilisateur
{
    [CsvRowNumber]
    public int Ligne { get; init; }

    [CsvRequired]
    [CsvPattern(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$")]
    public string Email { get; init; }

    [CsvRange(18, 99)]
    public int Age { get; init; }

    [CsvBoolean(["OUI", "1", "VRAI"], ["NON", "0", "FAUX"])]
    public bool Newsletter { get; init; }

    [CsvAllowedValues("STANDARD", "PREMIUM", "VIP")]
    public string NiveauAdhesion { get; init; }
}
```

### 2. Listes et tableaux dans une seule cellule

Si une colonne contient plusieurs valeurs séparées par un symbole distinct (ex. `;`), `[CsvDelimiterArray]` convertit automatiquement le contenu en tableau :

```csharp
[CsvSerializable]
public partial record struct FicheProduit
{
    public int Sku { get; init; }
    public string Libelle { get; init; }

    // Exemple CSV : "electronique;audio;bluetooth"
    // Converti directement en string[] sans étape manuelle
    [CsvDelimiterArray(delimiter: ';', escapeChar: '\\')]
    public string[] Tags { get; init; }
}
```

>NOTE: Si un élément contient le délimiteur interne (ex: une virgule), il suffit de le précéder de l'antislash dans le fichier CSV : `"Machin\, truc,Machin 2"`. FlashCsv retirera automatiquement l'échappement à la lecture et le protègera à l'écriture sans couper l'élément.

### 3. Sous-objets et sous-sous-objets aplatis (`[CsvFlatten]`)
L'attribut `[CsvFlatten]` permet de conserver un modèle C# propre et orienté objet (avec des sous-objets structurés) tout en manipulant un format CSV plat à deux dimensions (lignes et colonnes).

Le principe est simple : **le sous-objet n'est pas compressé dans une seule cellule, ses propriétés sont "dépliées" sous forme de colonnes directes** dans le fichier CSV.

Pour éviter les collisions de noms de colonnes entre plusieurs sous-objets (par exemple une adresse de facturation et une adresse de livraison), vous pouvez définir un préfixe dans `[CsvFlatten("prefix_")]`

```csharp
public partial record struct Adresse
{
    [CsvName("rue")]
    public string Rue { get; init; }

    [CsvName("code_postal", "cp")] // "code_postal" en écriture, "cp" accepté en lecture
    public string CodePostal { get; init; }

    [CsvName("ville")]
    public string Ville { get; init; }
}

// 2. Le modèle principal
[CsvSerializable]
public partial record struct CommandeClient
{
    [CsvName("id_commande")]
    public int Id { get; init; }

    [CsvName("nom_client")]
    public string Client { get; init; }

    // Les colonnes de cette adresse seront préfixées par "facturation_"
    [CsvFlatten("facturation_")]
    public Adresse Facturation { get; init; }

    // Les colonnes de cette adresse seront préfixées par "livraison_"
    [CsvFlatten("livraison_")]
    public Adresse Livraison { get; init; }
}
```

#### 1. Fonctionnement en Écriture (Sérialisation)
Lors de la sérialisation, FlashCsv concatène automatiquement le préfixe de l'attribut avec le nom de chaque propriété du sous-objet pour générer les colonnes :

```cs
using FlashCsv.Core;

var commande = new CommandeClient
{
    Id = 101,
    Client = "Alice Dupont",
    Facturation = new Adresse
    {
        Rue = "10 rue de Paris",
        CodePostal = "75001",
        Ville = "Paris"
    },
    Livraison = new Adresse
    {
        Rue = "45 avenue des Fleurs",
        CodePostal = "69002",
        Ville = "Lyon"
    }
};

string csv = FlashCsvSerializer.SerializeToString(new[] { commande });
Console.WriteLine(csv);
```

**Fichier CSV produit :**
```txt
id_commande,nom_client,facturation_rue,facturation_code_postal,facturation_ville,livraison_rue,livraison_code_postal,livraison_ville
101,Alice Dupont,10 rue de Paris,75001,Paris,45 avenue des Fleurs,69002,Lyon
```

#### 2. Fonctionnement en Lecture (Désérialisation)
À la lecture, le Source Generator de FlashCsv recherche les colonnes correspondant à `préfixe + nom` ou `préfixe + alias`. Il instancie ensuite automatiquement l'objet parent et ses sous-objets sans réflexion :

```cs
using FlashCsv.Core;

// Exemple avec une colonne utilisant un alias : "facturation_cp" au lieu de "facturation_code_postal"
string csvEntrant = """
id_commande,nom_client,facturation_rue,facturation_cp,facturation_ville,livraison_rue,livraison_code_postal,livraison_ville
202,Bob Martin,12 bd Victor Hugo,06000,Nice,12 bd Victor Hugo,06000,Nice
""";

var options = new CsvReaderOptions { HasHeader = true };
List<CommandeClient> commandes = FlashCsvSerializer.Deserialize<CommandeClient>(csvEntrant, options);

CommandeClient c = commandes[0];

// Les sous-objets sont peuplés et directement utilisables en C# :
Console.WriteLine($"Commande #{c.Id} pour {c.Client}");
Console.WriteLine($"Facturé à : {c.Facturation.Rue}, {c.Facturation.CodePostal} {c.Facturation.Ville}");
Console.WriteLine($"Livré à   : {c.Livraison.Rue}, {c.Livraison.CodePostal} {c.Livraison.Ville}");
```

**Sortie console**:
```txt
Commande #202 pour Bob Martin
Facturé à : 12 bd Victor Hugo, 06000 Nice
Livré à   : 12 bd Victor Hugo, 06000 Nice
```

#### 3. Utilisation sans préfixe (`[CsvFlatten]`)
```cs
public partial record struct AuditInfos
{
    [CsvName("cree_par")]
    public string CreePar { get; init; }

    [CsvName("cree_le")]
    public DateTime CreeLe { get; init; }
}

[CsvSerializable]
public partial record struct Article
{
    [CsvName("sku")]
    public string Sku { get; init; }

    // Sans préfixe : les colonnes s'appelleront directement "cree_par" et "cree_le"
    [CsvFlatten]
    public AuditInfos Audit { get; init; }
}
```

**CSV correspondant**:
```txt
sku,cree_par,cree_le
ART-404,Admin,2026-11-02T10:30:00Z
```

#### 4. Points clés à retenir sur [CsvFlatten]
- **Aplatissement pur** : Contrairement à `[CsvJson]` qui compacte du JSON dans une seule cellule, `[CsvFlatten]` distribue chaque champ dans sa propre colonne CSV.   
- **Cascadable** : Vous pouvez imbriquer des sous-objets sur plusieurs niveaux (un sous-objet qui contient lui-même un sous-sous-objet avec `[CsvFlatten]`), les préfixes s'additionnent alors de gauche à droite (ex. `client_adresse_rue`).   
- **Support des alias** : Tous les alias déclarés via `[CsvName]` ou redirigés dynamiquement via `ColumnMapping` fonctionnent avec le préfixe composé.   

### 4. Mapping dynamique d'import (`ColumnMapping`)

Lorsque vous consommez des exports externes dont les en-têtes changent selon les pays :

```csharp
var options = new CsvReaderOptions
{
    // Correspondance : ["NomProprieteCSharp"] = "NomColonneDansLeFichier"
    ColumnMapping = new Dictionary<string, string>
    {
        ["Id"] = "customer_identifier",
        ["Nom"] = "full_name",
        ["Solde"] = "remaining_balance"
    }
};

await using var stream = File.OpenRead("export_externe.csv");
await foreach (var client in FlashCsvSerializer.DeserializeAsync<Client>(stream, options))
{
    Console.WriteLine($"Client #{client.Id} - {client.Nom}");
}
```

> **Ordre de priorité en lecture :** `ColumnMapping` (dynamique) **>** `[CsvName]` (alias statiques) **>** Nom de la propriété C#.

### 5. Traduction et renommage des en-têtes (`FormatHeader`)

Lors de la sérialisation, transformez ou traduisez dynamiquement les noms des colonnes :

```csharp
var traductions = new Dictionary<string, string>
{
    ["id_client"] = "client_id_export",
    ["nom"] = "customer_name"
};

var options = new CsvWriterOptions
{
    Delimiter = (byte)';',
    WriteHeader = true,
    FormatHeader = nomOriginal => traductions.TryGetValue(nomOriginal, out var traduit) 
        ? traduit 
        : nomOriginal
};

string csv = FlashCsvSerializer.SerializeToString(clients, options);
```

> **Ordre de priorité en écriture :** `FormatHeader` reçoit le 1er nom de `[CsvName]` (ou le nom C#) et produit le libellé final écrit dans le fichier.

### 6. Transformation globale à la volée (`TransformField`)

L'option `TransformField` permet d'intercepter et modifier dynamiquement la valeur textuelle brute de n'importe quelle cellule, sans devoir altérer votre modèle de données C# ni créer de convertisseur dédié.

Sa signature prend le nom de la colonne et la valeur brute sous forme de chaîne :  
`Func<string, string, string>` ➔ `(colonne, valeur) => valeurTransformée`.

#### A. En lecture (`CsvReaderOptions.TransformField`)

Idéal pour normaliser des chaînes à la volée (par exemple passer des codes pays en majuscules, supprimer des caractères indésirables ou remplacer des valeurs sentinelles avant le parsing) :

```cs
using FlashCsv.Core;

var options = new CsvReaderOptions
{
    // Normalise la colonne "Pays" en majuscules et retire les espaces superflus de l'email
    TransformField = (colonne, valeur) => colonne switch
    {
        "Pays" => valeur.ToUpperInvariant(),
        "Email" => valeur.Trim().ToLowerInvariant(),
        _ => valeur
    }
};

await foreach (var client in FlashCsvSerializer.DeserializeAsync<Client>(stream, options))
{
    // client.Pays sera systématiquement en MAJUSCULES (ex: "FRANCE")
    // client.Email sera nettoyé et en minuscules
}
```

#### B. En écriture (CsvWriterOptions.TransformField)
Très pratique pour masquer des données sensibles (RGPD / anonymisation d'export) ou appliquer un formatage spécifique sur une colonne avant l'écriture dans le fichier :

```cs
using FlashCsv.Core;

var options = new CsvWriterOptions
{
    WriteHeader = true,
    // Masque partiellement les adresses email et anonymise les identifiants
    TransformField = (colonne, valeur) => colonne switch
    {
        "Email" when valeur.Contains('@') => 
            $"{valeur[..2]}***@{valeur.Split('@')[1]}", // "al***@domaine.com"
        
        "Rib" or "Iban" => 
            "**** CONFIDENTIEL ****",
            
        _ => valeur
    }
};

string csvAnonymise = FlashCsvSerializer.SerializeToString(clients, options);
```

>Bon à savoir :  
`TransformField` **s'exécute en premier** : FlashCsv applique automatiquement les guillemets et les sécurités anti-formule **après** votre transformation. Vous n'avez donc pas besoin d'échapper manuellement le texte renvoyé.

## Cas d'usage Avancés & Spécialisés

### 1. Insertion massive en Base de données (`SqlBulkCopy`)

FlashCsv intègre `CsvDataReader<T>` (dérivé de `DbDataReader`), permettant de streamer des millions de lignes directement dans une base SQL sans charger le fichier en mémoire :

```csharp
using Microsoft.Data.SqlClient;
using FlashCsv.Core;

await using var stream = File.OpenRead("gros_fichier_10M.csv");

// 1. Crée le flux asynchrone
var asyncSequence = FlashCsvSerializer.DeserializeAsync<Transaction>(stream);

// 2. Encapsule dans le DbDataReader AOT
using var dataReader = new CsvDataReader<Transaction>(asyncSequence);

// 3. Déversement direct et continu dans SQL Server
using var bulkCopy = new SqlBulkCopy("Server=localhost;Database=MaBase;Trusted_Connection=True;");
bulkCopy.DestinationTableName = "Transactions";
bulkCopy.BatchSize = 10_000;

await bulkCopy.WriteToServerAsync(dataReader);
```

### 2. Objets et listes d'objets complexes en JSON (AOT)

Pour stocker des listes ou des sous-arbres d'objets complexes au sein d'une seule cellule CSV sans perdre les bénéfices du Native AOT :

```csharp
using System.Text.Json.Serialization;
using FlashCsv.Annotations;

public record struct OptionItem(string Libelle, decimal Prix);

// Contexte JSON Native AOT
[JsonSerializable(typeof(List<OptionItem>))]
public partial class PanierJsonContext : JsonSerializerContext { }

[CsvSerializable]
public partial record struct Commande
{
    public int CommandeId { get; init; }

    // La cellule CSV contiendra du JSON compact désérialisé sans réflexion
    [CsvJson("options_json", JsonSourceGenContext = typeof(PanierJsonContext))]
    public List<OptionItem> Options { get; init; }
}
```

### 3. Polymorphisme et types dérivés

Un fichier CSV ne contient généralement qu'un seul type d'objet par fichier (ex: une liste de clients).  
Cependant, dans de nombreux systèmes réels (fichiers bancaires, journaux d'audit, imports ERP), **un même fichier CSV mélange plusieurs types d'enregistrements différents**.  
Considérez ce relevé bancaire où chaque ligne représente une opération, mais avec des informations différentes selon la nature de l'opération :

```txt
type_mouvement,id,montant,banque_origine,destinataire
CREDIT,101,150.00,BNP Paribas,
DEBIT,102,42.50,,Amazon
CREDIT,103,1200.00,Société Générale,
```

- Une ligne `CREDIT` possède une propriété `banque_origine`.
- Une ligne `DEBIT` possède une propriété `destinataire`.

En C# standard, vouloir désérialiser ce fichier vers une classe unique forcerait à créer une classe "fourre-tout" pleine de propriétés nullables.

Grâce à `[CsvDiscriminator]` et `[CsvDerivedType]`, FlashCsv inspecte la colonne discriminante (ici `type_mouvement`) et instancie automatiquement la classe fille correspondante, le tout sans réflexion et compatible Native AOT.

```csharp
using FlashCsv.Annotations;

// On définit une classe ou un record de base abstract, puis les classes dérivées concrètes.

// 1. Classe de base commune
[CsvSerializable]
[CsvDiscriminator("type_mouvement")] // Nom de la colonne qui indique le type de la ligne
[CsvDerivedType(typeof(Versement), "CREDIT")] // Si la valeur vaut "CREDIT" => instancie Versement
[CsvDerivedType(typeof(Retrait), "DEBIT")]    // Si la valeur vaut "DEBIT"  => instancie Retrait
public abstract partial record MouvementBancaire
{
    [CsvName("id")]
    public long Id { get; init; }

    [CsvName("montant")]
    public decimal Montant { get; init; }
}

// 2. Première variante concrète
public partial record Versement : MouvementBancaire
{
    [CsvName("banque_origine")]
    public string BanqueOrigine { get; init; } = "";
}

// 3. Deuxième variante concrète
public partial record Retrait : MouvementBancaire
{
    [CsvName("destinataire")]
    public string Destinataire { get; init; } = "";
}
```

#### 1. Lecture du flux (Désérialisation)
Lors du parcours du fichier avec `DeserializeAsync<MouvementBancaire>`, FlashCsv retourne des instances typées que vous pouvez inspecter avec du pattern matching C# standard (`switch` / `is`) :

```cs
using FlashCsv.Core;

await using var stream = File.OpenRead("releve_operations.csv");

await foreach (MouvementBancaire mouvement in FlashCsvSerializer.DeserializeAsync<MouvementBancaire>(stream))
{
    switch (mouvement)
    {
        case Versement credit:
            Console.WriteLine($"[+ CRÉDIT] #{credit.Id} de {credit.Montant} € reçu depuis {credit.BanqueOrigine}");
            break;

        case Retrait debit:
            Console.WriteLine($"[- DÉBIT]  #{debit.Id} de {debit.Montant} € payé à {debit.Destinataire}");
            break;
    }
}

/* Sortie:
* [+ CRÉDIT] #101 de 150.00 € reçu depuis BNP Paribas
* [- DÉBIT]  #102 de 42.50 € payé à Amazon
* [+ CRÉDIT] #103 de 1200.00 € reçu depuis Société Générale
*/
```

#### 2. Écriture du flux (Sérialisation)
À l'écriture, FlashCsv détecte automatiquement le type réel de chaque objet dans la liste hétérogène, renseigne automatiquement la colonne discriminante (`CREDIT` ou `DEBIT`) et écrit les colonnes spécifiques :

```cs
var operations = new MouvementBancaire[]
{
    new Versement { Id = 101, Montant = 150.00m, BanqueOrigine = "BNP Paribas" },
    new Retrait   { Id = 102, Montant = 42.50m, Destinataire = "Amazon" }
};

string csvSortie = FlashCsvSerializer.SerializeToString(operations);
```

#### Points importants à retenir
- **Zéro réflexion au runtime** : Le discriminateur est inspecté à l'aide d'un automate généré à la compilation par Roslyn, ce qui conserve les mêmes performances qu'un modèle plat.

- **Sécurité à la lecture** : Si une ligne contient une valeur de discriminant inconnue (ex: `"VIREMENT"` alors que seuls `"CREDIT"` et `"DEBIT"` sont déclarés), une erreur `UnknownDiscriminator` est transmise directement au gestionnaire `OnError`.

- **Les types enfants doivent être `partial`** pour permettre au générateur de lier leurs propriétés au schéma d'en-tête global.

### 4. Règles conditionnelles sans allocation (`ICsvCondition<T>`)

```csharp
using FlashCsv.Annotations;

// Condition statique zéro-allocation
public readonly struct EstProfessionnelCondition : ICsvCondition<EntrepriseClient>
{
    public static bool IsSatisfied(in EntrepriseClient entity) => entity.EstProfessionnel;
}

[CsvSerializable]
public partial record struct EntrepriseClient
{
    public string RaisonSociale { get; init; }
    public bool EstProfessionnel { get; init; }

    // Obligatoire uniquement si EstProfessionnel == true
    [CsvRequiredIf<EstProfessionnelCondition>]
    public string? NumeroTvaIntracommunautaire { get; init; }
}
```

### 5. Convertisseur binaire simple (`ICsvFieldReader<T>`)

Exemple concret : convertir une cellule contenant un montant avec sa devise (ex: `"150€"`) directement en entier numérique (`150`) sans allouer de chaîne :

```csharp
using System.Buffers.Text;
using FlashCsv.Annotations;

public sealed class MontantEuroReader : ICsvFieldReader<int>
{
    // source représente les octets bruts de la cellule dans le fichier CSV
    public static bool TryParse(
        ReadOnlySpan<byte> source, 
        out int value, 
        out CsvErrorCode errorCode)
    {
        // 1. Si la cellule se termine par le symbole '€' ou ' ', on le retire
        // (En UTF-8, le caractère '€' prend 3 octets : 0xE2, 0x82, 0xAC)
        if (source.EndsWith("€"u8))
        {
            source = source[..^3];
        }

        // 2. On parse les chiffres restants en UTF-8 sans créer de string
        if (Utf8Parser.TryParse(source, out value, out int bytesConsumed) 
            && bytesConsumed == source.Length)
        {
            errorCode = CsvErrorCode.None;
            return true; // Succès !
        }

        // 3. En cas de format invalide
        value = 0;
        errorCode = CsvErrorCode.InvalidInteger;
        return false;
    }
}

[CsvSerializable]
public partial record struct Facture
{
    public int Id { get; init; }

    [CsvConverter<MontantEuroReader>]
    public int Total { get; init; }
}
```

## Astuces pour les Confirmés & Experts

### 1. Utilisez des `readonly record struct` pour les flux massifs

Déclarer vos entités comme des structures évite les allocations d'objets sur le tas lors du `await foreach` :
```csharp
[CsvSerializable]
public readonly partial record struct LigneAudit(int Id, double Valeur);
```
Chaque itération recycle la mémoire de pile (Stack), supprimant les temps de pause GC (*Garbage Collection pauses*).

### 2. Le String Pooling (`UseStringPooling`)

Activé par défaut (`true`), `CsvStringPool` calcule un hash vectorisé sur les octets UTF-8 et résout la `string` correspondante sans appel à `Encoding.UTF8.GetString(...)`. Pour les colonnes à valeurs répétitives (pays, statuts, devises), la consommation mémoire est divisée par 10.

### 3. Sniffing automatique de délimiteur (`DelimiterSniffer`)

Pour traiter des fichiers dont le séparateur est imprévisible (`,`, `;`, `\t`, `|`), laissez `Delimiter = null` :
```csharp
var options = new CsvReaderOptions
{
    Delimiter = null // Détection automatique sur les 8 premiers Ko
};
```
Le sniffer vérifie la régularité du nombre de colonnes d'une ligne à l'autre pour déterminer le bon séparateur.

### 4. Afficher le code généré dans l'IDE (VSCodium / VS Code / Visual Studio)

Pour inspecter les fichiers générés par le Source Generator ou lever les avertissements d'éditeurs :
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### 5. Tests unitaires (xUnit) et cycle de vie des flux (`leaveOpen`)

Par défaut, les méthodes `SerializeAsync` et `DeserializeAsync` disposent (ferment) automatiquement le flux passé en paramètre (`leaveOpen = false` par défaut).

Si vous utilisez un `MemoryStream` dans des tests unitaires (par exemple avec **xUnit**) pour écrire puis relire immédiatement les données via `ms.Position = 0`, vous rencontrerez une exception `System.ObjectDisposedException` si vous ne conservez pas le flux ouvert:

```cs
[Fact]
public async Task Roundtrip_Memoire_ShouldPreserveData()
{
    var originaux = new[] { new Client { Id = 1, Nom = "Alice", Solde = 100.0 } };
    using var ms = new MemoryStream();

    // Définir leaveOpen: true pour éviter que SerializeAsync ne ferme ms à la fin
    await FlashCsvSerializer.SerializeAsync(ms, originaux, leaveOpen: true);

    // Repositionnement au début du flux sans exception
    ms.Position = 0;

    var resultats = new List<Client>();
    await foreach (var item in FlashCsvSerializer.DeserializeAsync<Client>(ms))
    {
        resultats.Add(item);
    }

    Assert.Single(resultats);
    Assert.Equal(originaux[0], resultats[0]);
}
```

## Référence complète des Options

### `CsvReaderOptions` (Lecture)

| Option | Type | Défaut | Description |
|:---|:---|:---|:---|
| `Delimiter` | `char?` | `','` | Délimiteur de champ (si `null`, active `DelimiterSniffer`). |
| `CandidateDelimiters` | `char[]` | `[',', ';', '\t', '\|']` | Séparateurs testés en mode auto-détection. |
| `Quote` | `byte` | `(byte)'"'` | Caractère d'encadrement des chaînes RFC 4180. |
| `CommentPrefix` | `byte?` | `null` | Caractère préfixant une ligne de commentaire (ex: `#`). |
| `HasHeader` | `bool` | `true` | Indique si la première ligne contient les noms des colonnes. |
| `TrimWhitespace` | `bool` | `false` | Supprime les espaces blancs en bordure de cellule avant parsing. |
| `IgnoreBlankLines` | `bool` | `true` | Ignore les retours à la ligne consécutifs ou vides. |
| `ErrorMode` | `CsvErrorMode` | `FailFast` | `FailFast` (stoppe dès l'erreur) ou `CollectAndContinue` (continue la lecture). |
| `OnError` | `CsvErrorHandler` | `null` | Délégué déclenché à chaque anomalie rencontrée. |
| `UseStringPooling` | `bool` | `true` | Active la mise en cache des chaînes de caractères répétitives. |
| `AllowCommaDecimalSeparator` | `bool` | `true` | Accepte et convertit automatiquement la virgule en point pour les nombres. |
| `MaxFieldSize` | `int` | `16 Mo` | Taille maximale permise pour une cellule (protection mémoire). |
| `BufferSize` | `int` | `64 Ko` | Taille du bloc mémoire alloué pour le pipeline de lecture. |
| `DateTimeFormat` | `string?` | `null` | Format de date par défaut lorsque aucun format local n'est défini. |
| `TrueValues` / `FalseValues` | `string[]` | Formats courants | Formes textuelles reconnues comme booléens valides. |
| `NullValues` | `string[]` | `["", "null", "\\N", ...]` | Valeurs textuelles équivalentes à `null`. |
| `IgnoreExtraColumns` | `bool` | `true` | Ignore silencieusement les colonnes surnuméraires sans lever d'erreur. |
| `ColumnMapping` | `Dictionary` | `null` | Dictionnaire de correspondance dynamique `[Propriété C#] => NomColonneCsv`. |
| `DetectAndSkipBom` | `bool` | `true` | Détecte et ignore le BOM UTF-8 (`0xEF`, `0xBB`, `0xBF`) inséré par Excel au début du fichier. |
| `DefaultHtmlSanitizeMode` | `HtmlSanitizeMode?` | `null` | Mode d'assainissement HTML appliqué par défaut à toutes les colonnes `string` (`StripTags` ou `Encode`). |
| `TransformField` | `Func<string, string, string>?` | `null` | Fonction globale de transformation appliquée aux cellules textuelles lues : `(nomColonne, valeurLue) => valeurModifiée`. |

### `CsvWriterOptions` (Écriture)

| Option | Type | Défaut | Description |
|:---|:---|:---|:---|
| `Delimiter` | `byte` | `(byte)','` | Séparateur de colonnes lors de la sérialisation. |
| `Quote` | `byte` | `(byte)'"'` | Caractère d'échappement / guillemet. |
| `QuoteStrategy` | `QuoteStrategy` | `Minimal` | `Minimal` (uniquement si requis), `Always` ou `Never`. |
| `EscapeFormulas` | `bool` | `false` | Préfixe d'un `'` les cellules débutant par `=`, `+`, `-`, `@`, `\t` ou `\r`. |
| `WriteHeader` | `bool` | `true` | Écrit la ligne d'en-tête au début du flux. |
| `NewLine` | `byte[]` | `\r\n` (CRLF) | Séquence d'octets marquant la fin de chaque enregistrement. |
| `FormatHeader` | `Func<string,string>` | `null` | Fonction optionnelle pour traduire ou reformater les libellés d'en-tête. |
| `NullFormat` | `string` | `""` | Chaîne de remplacement inscrite lorsqu'une valeur nulle est rencontrée. |
| `BufferSize` | `int` | `64 Ko` | Taille du buffer tampon avant vidage (`Flush`) vers le flux de destination. |
| `WriteBom` | `bool` | `false` | Écrit le BOM UTF-8 au début du fichier pour garantir la compatibilité des accents sous Microsoft Excel. |
| `TransformField` | `Func<string, string, string>?` | `null` | Fonction globale de transformation appliquée aux cellules textuelles avant écriture : `(nomColonne, valeurSource) => valeurModifiée`. |

## L'Exemple Ultime (All-in-One)

Voici un scénario métier complet regroupant l'ensemble des fonctionnalités clés de FlashCsv dans un cas d'usage réel : **un journal d'opérations e-commerce internationales avec gestion d'erreurs, Native AOT et zéro allocation**.

### 1. Le Modèle C# Complet

```csharp
using System.Buffers.Text;
using System.Text.Json.Serialization;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace MonApplication.Exports;

// A. Sous-objet aplati (Niveau 2 et 3)
public partial record struct CoordonneesGps
{
    [CsvName("lat")]
    public double Latitude { get; init; }

    [CsvName("lon")]
    public double Longitude { get; init; }
}

public partial record struct AdresseLivraison
{
    [CsvName("ville")]
    public string Ville { get; init; }

    // Sous-sous-objet aplati en cascade (colonnes: "livraison_gps_lat", "livraison_gps_lon")
    [CsvFlatten("gps_")]
    public CoordonneesGps Position { get; init; }
}

// B. Sous-objets complexes JSON (Native AOT)
public record struct ArticlePanier(string Sku, int Quantite);

[JsonSerializable(typeof(List<ArticlePanier>))]
public partial class PanierJsonContext : JsonSerializerContext { }

// C. Condition métier sans allocation
public readonly struct EstProfessionnelCondition : ICsvCondition<OperationCommerciale>
{
    public static bool IsSatisfied(in OperationCommerciale op) => op.EstSociete;
}

// D. Convertisseur binaire ultra-rapide sans allocation
public sealed class EuroMontantReader : ICsvFieldReader<int>
{
    public static bool TryParse(ReadOnlySpan<byte> source, out int value, out CsvErrorCode errorCode)
    {
        // Enlève le symbole "€" en fin de chaîne s'il existe
        if (source.EndsWith("€"u8))
            source = source[..^3];

        if (Utf8Parser.TryParse(source, out value, out int consumed) && consumed == source.Length)
        {
            errorCode = CsvErrorCode.None;
            return true;
        }

        value = 0;
        errorCode = CsvErrorCode.InvalidInteger;
        return false;
    }
}

// E. Énumération personnalisée
public enum StatutLivraison
{
    [CsvEnumMemberName("EN_ATTENTE", "ATTENTE", "PENDING")]
    EnAttente,

    [CsvEnumMemberName("EXPEDIE", "LIVRE", "SHIPPED")]
    Expedie
}

// F. Le Modèle Racine
[CsvSerializable]
public partial record struct OperationCommerciale
{
    // Métadonnées & numérotation automatique
    [CsvRowNumber]
    public int LigneFichier { get; init; }

    // Alias & validation stricte
    [CsvName("id_commande", "reference", "ref")]
    [CsvRequired]
    public int Id { get; init; }

    [CsvName("email_client")]
    [CsvPattern(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    [CsvSanitizeHtml(HtmlSanitizeMode.StripTags)] // Assainit le contenu HTML injecté
    public string Email { get; init; }

    // Convertisseur binaire sur mesure ("250€" -> 250)
    [CsvName("total_commande")]
    [CsvConverter<EuroMontantReader>]
    public int MontantTotal { get; init; }

    // Booléen tolérant plusieurs formats
    [CsvBoolean(["OUI", "TRUE", "1"], ["NON", "FALSE", "0"])]
    public bool EstSociete { get; init; }

    // Validation conditionnelle : obligatoire UNIQUEMENT si EstSociete == true
    [CsvRequiredIf<EstProfessionnelCondition>]
    public string? NumeroTva { get; init; }

    // Énumération avec alias
    [CsvEnum(CsvEnumFormat.Name)]
    public StatutLivraison Statut { get; init; }

    // Tableau de valeurs textuelles avec caractère d'échappement pour les virgules
    [CsvDelimiterArray(delimiter: ',', escapeChar: '\\')]
    public string[] Tags { get; init; }

    // Données JSON directes AOT
    [CsvJson("articles_json", JsonSourceGenContext = typeof(PanierJsonContext))]
    public List<ArticlePanier> Articles { get; init; }

    // Sous-objet aplati avec préfixe
    [CsvFlatten("livraison_")]
    public AdresseLivraison Livraison { get; init; }

    // Sécurité contre l'exécution de formules Excel (=SOMME, +cmd, etc.)
    [CsvSanitizeFormula]
    public string CommentaireClient { get; init; }

    // Capture des colonnes inconnues ou imprévues
    [CsvExtensionData]
    public Dictionary<string, string>? MetaDonneesInconnues { get; init; }
}
```

### 2. Le Fichier CSV Entrant (avec BOM UTF-8)
Ce fichier simule un export Excel contenant des particularités réelles :
- Préfixe BOM UTF-8 en tête de flux   
- Un élément échappé dans les tags (`"urgent\, prioritaire"`)
- Des cellules JSON avec guillemets RFC 4180
- Une formule malveillante dans le commentaire (`=1+1`)
- Une colonne non prévue (`date_creation_externe`)
- Une ligne corrompue (Ligne 3 : montant textuel invalide)

```txt
reference,email_client,total_commande,EstSociete,NumeroTva,Statut,Tags,articles_json,livraison_ville,livraison_gps_lat,livraison_gps_lon,CommentaireClient,date_creation_externe
101,<b>alice@test.com</b>,250€,1,FR123456789,LIVRE,"urgent\, prioritaire,fragile","[{""Sku"":""CLAVIER"",""Quantite"":1}]",Lyon,45.7640,4.8357,=1+1,2026-11-02
102,bob@domaine.com,ERREUR_MONTANT,0,,ATTENTE,"promo","[]",Paris,48.8566,2.3522,Normal,2026-11-02
103,charlie@pro.com,990€,OUI,FR987654321,SHIPPED,"express","[{""Sku"":""ECRAN"",""Quantite"":2}]",Marseille,43.2965,5.3698,RAS,2026-11-02
```

### 3. Exécution : Lecture, Résilience et Réécriture

```cs
using System.Text;
using FlashCsv.Core;

// Simulation d'un fichier avec BOM UTF-8
byte[] bom = [0xEF, 0xBB, 0xBF];
byte[] rawBytes = Encoding.UTF8.GetBytes(csvContenuBrut);
byte[] streamBytes = [..bom, ..rawBytes];

using var inputStream = new MemoryStream(streamBytes);

// 1. CONFIGURATION DE LECTURE ROBUSTE
var readerOptions = new CsvReaderOptions
{
    DetectAndSkipBom = true,                     // Ignore le BOM Excel transparent
    ErrorMode = CsvErrorMode.CollectAndContinue, // Ne plante pas sur la ligne de Bob (Ligne 3)
    OnError = err =>
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Ligne {err.LineNumber}] Champ '{err.ColumnName}' invalide ({err.Code}) -> Ligne ignorée.");
        Console.ResetColor();
    },
    // Mapping dynamique si les en-têtes changent au runtime
    ColumnMapping = new()
    {
        ["Id"] = "reference" // Prime sur les noms et alias du code C#
    },
    // Normalisation à la volée
    TransformField = (colonne, valeur) =>
    {
        if (colonne == "email_client") return valeur.Trim().ToLowerInvariant();
        return valeur;
    }
};

// Désérialisation en streaming continu
var operationsValides = new List<OperationCommerciale>();

await foreach (var op in FlashCsvSerializer.DeserializeAsync<OperationCommerciale>(inputStream, readerOptions))
{
    operationsValides.Add(op);

    Console.WriteLine($"Commande #{op.Id} (Ligne {op.LigneFichier}) :");
    Console.WriteLine($"   • Client       : {op.Email} (TVA: {op.NumeroTva ?? "N/A"})");
    Console.WriteLine($"   • Total parsé  : {op.MontantTotal} €");
    Console.WriteLine($"   • Premier Tag  : \"{op.Tags[0]}\" (échappement géré)");
    Console.WriteLine($"   • Statut       : {op.Statut}");
    Console.WriteLine($"   • Destination  : {op.Livraison.Ville} ({op.Livraison.Position.Latitude}, {op.Livraison.Position.Longitude})");
    Console.WriteLine($"   • Donnée extra : {op.MetaDonneesInconnues?["date_creation_externe"]}");
}

// 2. RÉÉCRITURE PROTÉGÉE POUR EXCEL
using var outputStream = new MemoryStream();

var writerOptions = new CsvWriterOptions
{
    WriteHeader = true,                          // Génère la première ligne de titre
    WriteBom = true,                             // Ajoute le BOM UTF-8 pour Excel
    EscapeFormulas = true,                       // Neutralise '=1+1' en '\'=1+1' pour contrer l'injection
    FormatHeader = nom => nom.ToUpperInvariant() // Écrit tous les en-têtes en MAJUSCULES
};

// leaveOpen: true pour pouvoir inspecter outputStream ensuite sans System.ObjectDisposedException
await FlashCsvSerializer.SerializeAsync(outputStream, operationsValides, writerOptions, leaveOpen: true);
```

**Sortie Console Générée**
```txt
Commande #101 (Ligne 2) :
   • Client       : alice@test.com (TVA: FR123456789)
   • Total parsé  : 250 €
   • Premier Tag  : "urgent, prioritaire" (échappement géré)
   • Statut       : Expedie
   • Destination  : Lyon (45.764, 4.8357)
   • Donnée extra : 2026-11-02
[Ligne 3] Champ 'total_commande' invalide (InvalidInteger) -> Ligne ignorée.
Commande #103 (Ligne 4) :
   • Client       : charlie@pro.com (TVA: FR987654321)
   • Total parsé  : 990 €
   • Premier Tag  : "express" (échappement géré)
   • Statut       : Expedie
   • Destination  : Marseille (43.2965, 5.3698)
   • Donnée extra : 2026-11-02
```

## Licence

Ce projet est distribué sous licence open-source **MIT**. Vous êtes libre de l'utiliser, le modifier et le distribuer dans vos projets personnels ou commerciaux.
