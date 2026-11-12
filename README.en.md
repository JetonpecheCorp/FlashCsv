# FlashCsv ⚡

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-100%25-brightgreen.svg)](#why-choose-flashcsv)
[![Zero Allocation](https://img.shields.io/badge/Memory-High%20Performance-orange.svg)](#performances--benchmarks)

<p align="center">
  <img src="assets/logoFlashCsv.png" alt="FlashCsv Logo" width="220" />
</p>

[English version](README.en.md) | [Version française disponible ici](README.md)

**FlashCsv** is an ultra-high-performance .NET library dedicated to CSV parsing, streaming, and serialization. Built from the ground up using **incremental C# Source Generators** (`IIncrementalGenerator`), it guarantees full **Native AOT** compatibility, completely eliminates runtime reflection, and minimizes memory allocations through low-level primitives (`ref struct`, `Span<byte>`, `ArrayPool<T>`).

---

## Table of Contents

1. [Why Choose FlashCsv?](#why-choose-flashcsv)
2. [Performances & Benchmarks](#performances--benchmarks)
3. [Installation](#installation)
4. [Quickstart (Beginner)](#quickstart-beginner)
   * [1. Declare your first model](#1-declare-your-first-model)
   * [2. Read from string or file](#2-read-from-string-or-file)
   * [3. Write to string or file](#3-write-to-string-or-file)
5. [Error Handling & Resilience (No Exceptions)](#error-handling--resilience-no-exceptions)
   * [Permissive mode (`CollectAndContinue`)](#1-permissive-mode-collectandcontinue--recommended-for-imports)
   * [Strict mode (`FailFast`)](#2-strict-mode-failfast)
6. [Architecture Diagrams & Processing Pipeline](#-architecture-diagrams)
   * [Deserialization Cycle (Reading)](#1-deserialization-cycle-reading)
   * [Serialization Cycle (Writing)](#2-serialization-cycle-writing)
7. [Comprehensive Attribute Guide](#comprehensive-attribute-guide)
   * [Structure & Columns](#structure--columns)
   * [Formats, Conversions & Collections](#formats-conversions--collections)
   * [Data Validation](#data-validation)
   * [Security & Sanitization](#security--sanitization)
8. [Everyday Practical Examples](#everyday-practical-examples)
   * [Business validation & bounds](#1-business-validation-and-bounds)
   * [Arrays and lists in a single cell (`[CsvDelimiterArray]`)](#2-lists-and-arrays-in-a-single-cell)
   * [Flattened nested objects (`[CsvFlatten]`)](#3-flattened-nested-objects-csvflatten)
   * [Dynamic import mapping (`ColumnMapping`)](#4-dynamic-import-mapping-columnmapping)
   * [Dynamic header translation on export (`FormatHeader`)](#5-header-translation-and-renaming-formatheader)
   * [Global on-the-fly field transformation (`TransformField`)](#6-global-on-the-fly-transformation-transformfield)
9. [Advanced & Specialized Scenarios](#-advanced--specialized-scenarios)
   * [Massive database ingestion (`SqlBulkCopy` / `CreateDataReader`)](#1-massive-database-insertion-sqlbulkcopy)
   * [Complex objects & JSON lists AOT (`[CsvJson]`)](#2-complex-objects-and-json-lists-aot)
   * [Polymorphism & derived types (`[CsvDiscriminator]`)](#3-polymorphism-and-derived-types)
   * [Zero-allocation conditional rules (`ICsvCondition<T>`)](#4-zero-allocation-conditional-rules-icsvconditiont)
   * [Binary custom field converter (`ICsvFieldReader<T>`)](#5-simple-binary-field-converter-icsvfieldreadert)
10. [Pro Tips for Power Users](#pro-tips-for-power-users)
11. [Complete Options Reference](#️-complete-options-reference)
    * [`CsvReaderOptions`](#csvreaderoptions-reading)
    * [`CsvWriterOptions`](#csvwriteroptions-writing)
12. [The Ultimate All-in-One Example](#the-ultimate-example-all-in-one)
13. [License](#license)

---

## Why Choose FlashCsv?

Most traditional CSV parsers rely heavily on **C# Reflection** (`Type.GetProperties()`, `Activator.CreateInstance()`). These mechanisms trigger massive allocations on the managed heap, choke the Garbage Collector (GC), and cause warnings or outright failure under **Native AOT** compilation.

| Criteria | Traditional Parsers | FlashCsv ⚡ |
|:---|:---|:---|
| **Native AOT Compilation** | Incompatible or Trimming warnings | **100% Native AOT & Trimming-Friendly** |
| **Code Generation** | Runtime Reflection / Dynamic IL Emit | **Roslyn C# Source Generator** at compile time |
| **Memory Management** | Multiple temporary strings & boxing | **Zero heap churn** (`ref struct`, `Span<byte>`, `ArrayPool`) |
| **Large Datasets (GBs)** | Risk of Out Of Memory (`OOM`) crashes | **Pure asynchronous streaming** via `IAsyncEnumerable<T>` |
| **Built-in Security** | Unfiltered Excel formula injection | **Automatic shielding** (`EscapeFormulas`, XSS encoding) |
| **SQL Database Feeding** | Requires intermediate lists or DataTable | **Native streaming `DbDataReader`** for `SqlBulkCopy` |

---

## Performances & Benchmarks

Official measurements conducted using **BenchmarkDotNet v0.15.8** on **Windows 11** (.NET SDK 10.0.403, RyuJIT x86-64-v4, Intel Core i5-1145G7 2.60GHz):

### 1. Dataset: 100,000 rows

| Operation | Library | Mean | Error | StdDev | Rank | Gen 0 | Gen 1 | Allocated |
|:---|:---|---:|---:|---:|:---:|---:|---:|---:|
| **Read** | **FlashCsv** ⚡ | **18.32 ms** | 0.318 ms | 0.435 ms | **2** | 937.5000 | - | **3.82 MB** |
| Read | Sylvan | 18.96 ms | 0.379 ms | 0.978 ms | 2 | 1000.0000 | 156.2500 | 4.04 MB |
| **Write** | **FlashCsv** ⚡ | **15.59 ms** | 0.312 ms | 0.485 ms | **1** | 750.0000 | - | **3.05 MB** |

### 2. Dataset: 1,000,000 rows (1 Million)

| Operation | Library | Mean | Error | StdDev | Rank | Gen 0 | Allocated |
|:---|:---|---:|---:|---:|:---:|---:|---:|
| **Read** | **FlashCsv** ⚡ | **183.61 ms** | 2.971 ms | 2.481 ms | **2** | 9333.3333 | **38.16 MB** |
| Read | Sylvan | 194.51 ms | 3.840 ms | 9.201 ms | 2 | 10000.0000 | 40.00 MB |
| **Write** | **FlashCsv** ⚡ | **159.01 ms** | 3.140 ms | 6.198 ms | **1** | 7500.0000 | **30.52 MB** |

> **Key Highlights:**
> - **Reading:** FlashCsv is **faster** than Sylvan (18.32 ms vs 18.96 ms on 100k; 183.61 ms vs 194.51 ms on 1M) while allocating less memory and triggering zero Gen 1 garbage collections.
> - **Writing:** FlashCsv writes **1 million rows in just ~159 ms** (Rank 1), with barely 30 MB allocated thanks to direct UTF-8 formatting without boxing.

---

## Installation

```bash
dotnet add package FlashCsv
```

---

## Quickstart (Beginner)

### 1. Declare your first model

Decorate your `class`, `struct`, or `record` with **`[CsvSerializable]`** and add the **`partial`** keyword:

```csharp
using FlashCsv.Annotations;

namespace MyApp;

[CsvSerializable]
public partial record struct Customer
{
    [CsvName("customer_id", "id")]
    public int Id { get; init; }

    [CsvName("name")]
    public string Name { get; init; }

    [CsvName("balance")]
    public double Balance { get; init; }
}
```

> 💡 **Why `partial`?** FlashCsv generates the other half of your class during compilation to implement zero-reflection AOT mappers and lightning-fast serialization without manual interfaces or boxing.

### 2. Read from string or file

#### In-memory from a string
```csharp
using FlashCsv.Core;

string csv = """
customer_id,name,balance
1,Alice,1250.50
2,Bob,3400.00
""";

List<Customer> customers = FlashCsvSerializer.Deserialize<Customer>(csv);

foreach (var c in customers)
{
    Console.WriteLine($"Customer #{c.Id}: {c.Name} (${c.Balance})");
}
```

#### Asynchronous streaming from a file (Massive datasets)
```csharp
using FlashCsv.Core;

await using var stream = File.OpenRead("customers_import.csv");

// Never loads the whole file into RAM: true row-by-row streaming!
await foreach (Customer c in FlashCsvSerializer.DeserializeAsync<Customer>(stream))
{
    Console.WriteLine($"Imported: {c.Name}");
}
```

### 3. Write to string or file

```csharp
using FlashCsv.Core;

var customers = new[]
{
    new Customer { Id = 1, Name = "Alice", Balance = 1250.50 },
    new Customer { Id = 2, Name = "Bob", Balance = 3400.00 }
};

// 1. Get as a string
string csvText = FlashCsvSerializer.SerializeToString(customers);

// 2. Or stream directly into a file
await using var outStream = File.Create("export_customers.csv");
await FlashCsvSerializer.SerializeAsync(outStream, customers);
```

---

## Error Handling & Resilience (No Exceptions)

In FlashCsv, error handling requires **no `try...catch` blocks**. Errors are routed directly to your `OnError` callback without breaking your pipeline with expensive exceptions.

### 1. Permissive mode (`CollectAndContinue`) — Recommended for imports

This mode silently bypasses malformed rows, invokes your `OnError` callback with contextual diagnostics, and continues processing the remaining rows:

```csharp
using FlashCsv.Core;

string invalidCsv = """
customer_id,name,balance
1,Alice,1250.50
2,Bob,one-hundred-dollars
3,Charlie,890.00
""";

var options = new CsvReaderOptions
{
    ErrorMode = CsvErrorMode.CollectAndContinue,
    OnError = error =>
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Error Row {error.LineNumber}] " +
                          $"Column '{error.ColumnName}': Code {error.Code}");
        Console.ResetColor();
    }
};

using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidCsv));

// Only Alice (#1) and Charlie (#3) will be yielded
await foreach (var customer in FlashCsvSerializer.DeserializeAsync<Customer>(stream, options))
{
    Console.WriteLine($"Valid: #{customer.Id} {customer.Name}");
}
```

**Console Output:**
```text
Valid: #1 Alice
[Error Row 3] Column 'balance': Code InvalidFloat
Valid: #3 Charlie
```

### 2. Strict mode (`FailFast`)

If any anomaly must immediately halt processing, `FailFast` notifies `OnError` and cleanly terminates enumeration without throwing:

```csharp
var options = new CsvReaderOptions
{
    ErrorMode = CsvErrorMode.FailFast, // Stop on the very first issue
    OnError = error =>
    {
        Console.WriteLine($"[Emergency Stop] Row {error.LineNumber} corrupt on {error.ColumnName}.");
    }
};

await foreach (var customer in FlashCsvSerializer.DeserializeAsync<Customer>(stream, options))
{
    // Will not receive any row after the malformed one
}
```

---

## 🔄 Architecture Diagrams

### 1. Deserialization Cycle (Reading)

```text
📥 RAW CSV STREAM (File, Memory, Network)
    │
    ▼
┌────────────────────────────────────────────────────────┐
│  STAGE 1 : BUFFER & STREAM (CsvStreamReader)           │
│  Options: BufferSize • CommentPrefix • BlankLines      │
│                                                        │
│  • Borrow zero-alloc buffer (ArrayPool<byte>)          │
│  • On-the-fly skipping of blank lines & comments (#)   │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STAGE 2 : FORMAT & HEADERS (DelimiterSniffer)         │
│  Options: Delimiter • CandidateDelimiters • HasHeader  │
│                                                        │
│  • Automatic delimiter sniffing if Delimiter == null   │
│  • Header row reading & column mapping                 │
│  • Application of ColumnMapping (priority over CsvName)│
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STAGE 3 : ULTRA-FAST TOKENIZATION (CsvRowTokenizer)   │
│  Options: Quote • MaxFieldSize • IgnoreExtraColumns    │
│                                                        │
│  • Zero-copy SIMD splitting via SearchValues<byte>     │
│  • RFC 4180 double-quote unescaping ("")               │
│  • Security guardrail: reject if cell > MaxFieldSize   │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STAGE 4 : CELL NORMALIZATION (CsvRowView)             │
│  Options: TrimWhitespace • NullValues • StringPooling  │
│                                                        │
│  • Edge trimming if enabled                            │
│  • Immediate null recognition ("NA", "\N", etc.)       │
│  • CsvStringPool deduplication for repeated strings    │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STAGE 5 : TYPED PARSING (Source Generator AOT)        │
│  Options: AllowCommaDecimalSeparator • DateTimeFormat  │
│                                                        │
│  • Numbers: Direct Utf8Parser (replaces ',' with '.')  │
│  • Dates: Binary ISO 8601 UTF-8 fast-path              │
│  • Validations: [CsvRange], [CsvPattern], etc.         │
└─────────────┬────────────────────────────┬─────────────┘
              │                            │
      [Parsing Success]            [Error Detected]
              │                            │
              ▼                            ▼
      INSTANCE T                  INCIDENT HANDLING
      (Yield Return)              Options: ErrorMode • OnError
                                  • Triggers OnError callback
                                  • CollectAndContinue ➔ skips row
                                  • FailFast ➔ stops stream
```

### 2. Serialization Cycle (Writing)

```text
IN-MEMORY OBJECTS (IEnumerable<T> / IAsyncEnumerable<T>)
    │
    ▼
┌────────────────────────────────────────────────────────┐
│  STEP 1 : HEADER ROW                                   │
│  Options: WriteHeader • FormatHeader • Delimiter       │
│                                                        │
│  • If WriteHeader == true: generate column titles      │
│  • [CsvName] supplies primary column name              │
│  • FormatHeader applies dynamic translation / renaming │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STEP 2 : PRIMITIVE FORMATTING (IUtf8SpanFormattable)  │
│  Options: NullFormat • TransformField                  │
│                                                        │
│  • Numbers / Dates: written directly to UTF-8 bytes    │
│  • JSON: serialized via Utf8JsonWriter without alloc   │
│  • If null: outputs NullFormat string                  │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STEP 3 : ANTI-INJECTION SECURITY                      │
│  Options: EscapeFormulas • [CsvSanitizeFormula]        │
│                                                        │
│  • Checks if cell starts with =, +, -, @, or \t        │
│  • Prefixes with single apostrophe (') for Excel/Calc  │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STEP 4 : RFC 4180 ESCAPING & QUOTING                  │
│  Options: QuoteStrategy • Quote • Delimiter            │
│                                                        │
│  • Minimal: quote only if delimiter, quote, or newline │
│  • Always: systematically quote all cells              │
│  • Automatic doubling of embedded quotes               │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│  STEP 5 : DISK / NETWORK FLUSH                         │
│  Options: NewLine • BufferSize                         │
│                                                        │
│  • Append delimiter bytes & NewLine sequence           │
│  • Automatic Flush of ArrayPool buffer                 │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
 PRODUCED CSV STREAM (UTF-8)
```

---

## Comprehensive Attribute Guide

### Structure & Columns

| Attribute | Target | Purpose |
|:---|:---|:---|
| `[CsvSerializable]` | Class / Struct / Record | Activates the Roslyn incremental Source Generator for this type. |
| `[CsvName("primary", "alias1", ...)]` | Property / Field | Primary name for writing, list of accepted aliases for reading. |
| `[CsvOrder(int)]` | Property / Field | Physical column ordering during serialization. |
| `[CsvRowNumber]` | Property (`int`, `long`) | Injects source CSV row index automatically (1-based). |
| `[CsvIgnore]` | Property / Field | Ignores property during reading and writing. |
| `[CsvFlatten("prefix_")]` | Property / Field | Flattens nested object properties into parent CSV columns. |
| `[CsvExtensionData]` | Property / Field | Captures unmatched extra columns into a `Dictionary<string, string>`. |

### Formats, Conversions & Collections

| Attribute | Target | Purpose |
|:---|:---|:---|
| `[CsvFormat("...")]` | Property / Field | Explicit formatting mask (e.g. `"yyyy-MM-dd"`, `"0.00"`). |
| `[CsvDelimiterArray(';')]` | Property / Field | Maps an array/list into a single cell with an internal delimiter. |
| `[CsvJson("col", ...)]` | Property / Field | Maps a cell containing JSON payload directly (AOT compatible). |
| `[CsvNullValues("NA", "\\N", ...)]` | Property / Field | Custom candidate strings recognized as `null` for this column. |
| `[CsvBoolean([...], [...])]` | Property / Field | Custom string values accepted for `true` and `false`. |
| `[CsvEnum(CsvEnumFormat)]` | Property / Field | Enum serialization by literal `Name` or integer `UnderlyingValue`. |
| `[CsvEnumMemberName("Val", ...)]` | Enum Member | Primary serialization name and deserialization aliases for an enum value. |
| `[CsvConverter<TConverter>]` | Property / Field | Attaches a custom binary reader/writer without reflection. |

> The `[CsvFormat("...")]` attribute applies to **all temporal, numerical, and identifier types** supported by FlashCsv:
> `DateTime`, `DateOnly`, `TimeOnly`, `decimal`, `double`, `int`, `Guid`.  
> In short, it works on **any .NET type implementing** `ISpanFormattable` / `IFormattable`.

### Data Validation

| Attribute | Target | Purpose |
|:---|:---|:---|
| `[CsvRequired]` | Property / Field | Field must not be missing or empty (`RequiredFieldMissing`). |
| `[CsvRequiredIf<TCondition>]` | Property / Field | Zero-allocation conditional requirement evaluated statically. |
| `[CsvRange(min, max)]` | Property / Field | Validates numeric value within `[min, max]` range (inclusive). |
| `[CsvMin(val)]` / `[CsvMax(val)]` | Property / Field | Numeric lower or upper bound. |
| `[CsvLength(min, max)]` | Property / Field | Validates string length between two bounds. |
| `[CsvMinLength(n)]` / `[CsvMaxLength(n)]` | Property / Field | Minimum or maximum string length. |
| `[CsvPattern(@"regex")]` | Property / Field | Validates field against regular expression. |
| `[CsvAllowedValues("A", "B", ...)]` | Property / Field | Strict whitelist of permitted text values. |

### Security & Sanitization

| Attribute | Target | Purpose |
|:---|:---|:---|
| `[CsvSanitizeFormula]` | Property / Field | Prefixes with `'` if cell starts with `=`, `+`, `-`, `@`, `\t`, or `\r`. |
| `[CsvSanitizeHtml(mode)]` | Property / Field | Encodes or strips HTML tags (`StripTags` / `Encode`). |
| `[CsvCase(CsvCaseMode)]` | Property / Field | Normalizes text casing (`Upper`, `Lower`, `Title`). |

---

## Everyday Practical Examples

### 1. Business validation and bounds

```csharp
using FlashCsv.Annotations;

[CsvSerializable]
public partial record struct UserProfile
{
    [CsvRowNumber]
    public int RowIndex { get; init; }

    [CsvRequired]
    [CsvPattern(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$")]
    public string Email { get; init; }

    [CsvRange(18, 99)]
    public int Age { get; init; }

    [CsvBoolean(["YES", "1", "TRUE"], ["NO", "0", "FALSE"])]
    public bool Newsletter { get; init; }

    [CsvAllowedValues("STANDARD", "PREMIUM", "VIP")]
    public string MembershipTier { get; init; }
}
```

### 2. Lists and arrays in a single cell

If a column stores multiple values separated by an internal symbol (e.g. `;`), `[CsvDelimiterArray]` maps it directly into an array:

```csharp
[CsvSerializable]
public partial record struct ProductRecord
{
    public int Sku { get; init; }
    public string Title { get; init; }

    // CSV sample: "electronics;audio;bluetooth"
    // Converted directly into string[] without manual steps
    [CsvDelimiterArray(delimiter: ';', escapeChar: '\\')]
    public string[] Tags { get; init; }
}
```

> **Note:** If an item contains the delimiter, simply escape it with a backslash in the CSV: `"gadget\, tool;accessory"`. FlashCsv unescapes it on read and protects it on write without splitting.

### 3. Flattened nested objects (`[CsvFlatten]`)

`[CsvFlatten]` allows you to maintain clean, object-oriented domain models (with nested structs/objects) while reading and writing 2D flat CSV files.

The nested object is not compressed into a single cell; **its properties are expanded as direct individual columns in the CSV file**.

Use prefixes to prevent column name collisions (e.g. between billing and shipping addresses):

```csharp
public partial record struct Address
{
    [CsvName("street")]
    public string Street { get; init; }

    [CsvName("zip_code", "zip")] // "zip_code" for write, "zip" accepted for read
    public string ZipCode { get; init; }

    [CsvName("city")]
    public string City { get; init; }
}

[CsvSerializable]
public partial record struct CustomerOrder
{
    [CsvName("order_id")]
    public int Id { get; init; }

    [CsvName("customer_name")]
    public string Customer { get; init; }

    // Columns prefixed with "billing_"
    [CsvFlatten("billing_")]
    public Address Billing { get; init; }

    // Columns prefixed with "shipping_"
    [CsvFlatten("shipping_")]
    public Address Shipping { get; init; }
}
```

#### Writing (Serialization)
```csharp
using FlashCsv.Core;

var order = new CustomerOrder
{
    Id = 101,
    Customer = "Alice Dupont",
    Billing = new Address { Street = "10 Baker St", ZipCode = "W1U", City = "London" },
    Shipping = new Address { Street = "45 Park Ave", ZipCode = "10022", City = "New York" }
};

string csv = FlashCsvSerializer.SerializeToString(new[] { order });
```

**Produced CSV:**
```txt
order_id,customer_name,billing_street,billing_zip_code,billing_city,shipping_street,shipping_zip_code,shipping_city
101,Alice Dupont,10 Baker St,W1U,London,45 Park Ave,10022,New York
```

#### Reading (Deserialization)
```csharp
string incomingCsv = """
order_id,customer_name,billing_street,billing_zip,billing_city,shipping_street,shipping_zip_code,shipping_city
202,Bob Martin,12 Ocean Dr,33139,Miami,12 Ocean Dr,33139,Miami
""";

var options = new CsvReaderOptions { HasHeader = true };
List<CustomerOrder> orders = FlashCsvSerializer.Deserialize<CustomerOrder>(incomingCsv, options);
```

#### Key points about `[CsvFlatten]`:
- **True column expansion**: Unlike `[CsvJson]` which serializes into one JSON cell, `[CsvFlatten]` maps each property to its own CSV column.
- **Cascadable**: Nest objects across multiple levels; prefixes concatenate cleanly (e.g. `client_address_street`).
- **Alias support**: Aliases declared via `[CsvName]` or mapped dynamically via `ColumnMapping` work transparently with prefixes.

### 4. Dynamic import mapping (`ColumnMapping`)

When importing third-party files whose header names differ from your C# model:

```csharp
var options = new CsvReaderOptions
{
    // Mapping: ["CSharpPropertyName"] = "ActualCsvHeaderName"
    ColumnMapping = new Dictionary<string, string>
    {
        ["Id"] = "customer_identifier",
        ["Name"] = "full_name",
        ["Balance"] = "remaining_balance"
    }
};

await using var stream = File.OpenRead("external_export.csv");
await foreach (var client in FlashCsvSerializer.DeserializeAsync<Customer>(stream, options))
{
    Console.WriteLine($"Customer #{client.Id} - {client.Name}");
}
```

> **Reading Priority:** `ColumnMapping` (dynamic) **>** `[CsvName]` (static aliases) **>** C# Property Name.

### 5. Header translation and renaming (`FormatHeader`)

Dynamically rename or translate headers when exporting:

```csharp
var translations = new Dictionary<string, string>
{
    ["customer_id"] = "id_client_export",
    ["name"] = "nom_client"
};

var options = new CsvWriterOptions
{
    Delimiter = (byte)';',
    WriteHeader = true,
    FormatHeader = originalName => translations.TryGetValue(originalName, out var translated) 
        ? translated 
        : originalName
};

string csv = FlashCsvSerializer.SerializeToString(customers, options);
```

### 6. Global on-the-fly transformation (`TransformField`)

The `TransformField` option intercepts raw string values dynamically without altering your C# model:
`Func<string, string, string>` ➔ `(columnName, rawValue) => modifiedValue`.

#### Reading (`CsvReaderOptions.TransformField`)
```csharp
var options = new CsvReaderOptions
{
    TransformField = (column, value) => column switch
    {
        "Country" => value.ToUpperInvariant(),
        "Email" => value.Trim().ToLowerInvariant(),
        _ => value
    }
};
```

#### Writing (`CsvWriterOptions.TransformField`)
Ideal for masking PII or GDPR compliance:
```csharp
var options = new CsvWriterOptions
{
    TransformField = (column, value) => column switch
    {
        "Email" when value.Contains('@') => 
            $"{value[..2]}***@{value.Split('@')[1]}",
        "Iban" => "**** CONFIDENTIAL ****",
        _ => value
    }
};
```

---

## 🚀 Advanced & Specialized Scenarios

### 1. Massive database insertion (`SqlBulkCopy`)

FlashCsv provides `CreateDataReader<T>` (derived from `DbDataReader`), streaming millions of records directly into SQL Server without intermediate memory allocation:

```csharp
using Microsoft.Data.SqlClient;
using FlashCsv.Core;

await using var stream = File.OpenRead("large_10M_dataset.csv");

// 1. Direct DbDataReader creation from the CSV stream (or an existing sequence)
using var dataReader = FlashCsvSerializer.CreateDataReader<TransactionRecord>(stream);

// 2. Direct continuous streaming into SQL Server
using var bulkCopy = new SqlBulkCopy("Server=localhost;Database=MyDb;Trusted_Connection=True;");
bulkCopy.DestinationTableName = "Transactions";
bulkCopy.BatchSize = 10_000;

await bulkCopy.WriteToServerAsync(dataReader);
```

### 2. Complex objects and JSON lists (AOT)

To store nested subtrees in a single cell with 100% Native AOT compliance:

```csharp
using System.Text.Json.Serialization;
using FlashCsv.Annotations;

public record struct CartItem(string Sku, int Quantity);

// Native AOT JSON context
[JsonSerializable(typeof(List<CartItem>))]
public partial class CartJsonContext : JsonSerializerContext { }

[CsvSerializable]
public partial record struct Order
{
    public int OrderId { get; init; }

    [CsvJson("items_json", JsonSourceGenContext = typeof(CartJsonContext))]
    public List<CartItem> Items { get; init; }
}
```

### 3. Polymorphism and derived types

When a single CSV file contains multiple heterogeneous record types (e.g. banking transactions, audit logs):

```txt
record_type,id,amount,source_bank,recipient
CREDIT,101,150.00,Barclays,
DEBIT,102,42.50,,Amazon
CREDIT,103,1200.00,HSBC,
```

```csharp
using FlashCsv.Annotations;

// 1. Abstract base record
[CsvSerializable]
[CsvDiscriminator("record_type")]
[CsvDerivedType(typeof(CreditRecord), "CREDIT")]
[CsvDerivedType(typeof(DebitRecord), "DEBIT")]
public abstract partial record BankTransaction
{
    [CsvName("id")]
    public long Id { get; init; }

    [CsvName("amount")]
    public decimal Amount { get; init; }
}

// 2. Derived variants
public partial record CreditRecord : BankTransaction
{
    [CsvName("source_bank")]
    public string SourceBank { get; init; } = "";
}

public partial record DebitRecord : BankTransaction
{
    [CsvName("recipient")]
    public string Recipient { get; init; } = "";
}
```

#### Reading
```csharp
await foreach (BankTransaction tx in FlashCsvSerializer.DeserializeAsync<BankTransaction>(stream))
{
    switch (tx)
    {
        case CreditRecord credit:
            Console.WriteLine($"[+ CREDIT] #{credit.Id}: ${credit.Amount} from {credit.SourceBank}");
            break;
        case DebitRecord debit:
            Console.WriteLine($"[- DEBIT]  #{debit.Id}: ${debit.Amount} paid to {debit.Recipient}");
            break;
    }
}
```

### 4. Zero-allocation conditional rules (`ICsvCondition<T>`)

```csharp
using FlashCsv.Annotations;

public readonly struct IsBusinessCondition : ICsvCondition<CompanyClient>
{
    public static bool IsSatisfied(in CompanyClient entity) => entity.IsBusiness;
}

[CsvSerializable]
public partial record struct CompanyClient
{
    public string Name { get; init; }
    public bool IsBusiness { get; init; }

    // Required ONLY if IsBusiness == true
    [CsvRequiredIf<IsBusinessCondition>]
    public string? VatNumber { get; init; }
}
```

### 5. Simple binary field converter (`ICsvFieldReader<T>`)

Parse custom currency formats (e.g. `"150€"`) directly into an `int` without allocating a single string:

```csharp
using System.Buffers.Text;
using FlashCsv.Annotations;

public sealed class EuroAmountReader : ICsvFieldReader<int>
{
    public static bool TryParse(
        ReadOnlySpan<byte> source, 
        out int value, 
        out CsvErrorCode errorCode)
    {
        // 1. Strip trailing '€' if present (3 bytes in UTF-8: 0xE2, 0x82, 0xAC)
        if (source.EndsWith("€"u8))
            source = source[..^3];

        // 2. Parse UTF-8 bytes directly
        if (Utf8Parser.TryParse(source, out value, out int bytesConsumed) 
            && bytesConsumed == source.Length)
        {
            errorCode = CsvErrorCode.None;
            return true;
        }

        value = 0;
        errorCode = CsvErrorCode.InvalidInteger;
        return false;
    }
}

[CsvSerializable]
public partial record struct Invoice
{
    public int Id { get; init; }

    [CsvConverter<EuroAmountReader>]
    public int Total { get; init; }
}
```

---

## Pro Tips for Power Users

### 1. Prefer `readonly record struct` for large streams
Using structs recycles stack memory during enumeration and eliminates Garbage Collection pauses:
```csharp
[CsvSerializable]
public readonly partial record struct AuditEntry(int Id, double Value);
```

### 2. String Pooling (`UseStringPooling`)
Enabled by default (`true`), `CsvStringPool` hashes UTF-8 spans with SIMD vectors to reuse strings without allocating new ones for repetitive columns (countries, categories, status codes).

### 3. Automatic Delimiter Sniffing (`DelimiterSniffer`)
To handle files with uncertain delimiters (`,`, `;`, `\t`, `|`), simply leave `Delimiter = null`:
```csharp
var options = new CsvReaderOptions { Delimiter = null };
```

### 4. Viewing generated code in your IDE
To inspect code emitted by the Roslyn Source Generator:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### 5. Unit testing & stream lifecycle (`leaveOpen`)
By default, `SerializeAsync` and `DeserializeAsync` dispose the underlying stream (`leaveOpen = false`). When testing with `MemoryStream` in xUnit, pass `leaveOpen: true`:
```csharp
await FlashCsvSerializer.SerializeAsync(ms, data, leaveOpen: true);
ms.Position = 0; // Rewind without ObjectDisposedException
```

---

## ⚙️ Complete Options Reference

### `CsvReaderOptions` (Reading)

| Option | Type | Default | Description |
|:---|:---|:---|:---|
| `Delimiter` | `char?` | `','` | Field delimiter (if `null`, uses `DelimiterSniffer`). |
| `CandidateDelimiters` | `char[]` | `[',', ';', '\t', '\|']` | Candidate delimiters tested during auto-sniffing. |
| `Quote` | `byte` | `(byte)'"'` | Quote character for RFC 4180 escaping. |
| `CommentPrefix` | `byte?` | `null` | Prefix character marking comment rows (e.g. `#`). |
| `HasHeader` | `bool` | `true` | Indicates whether the first non-empty line contains headers. |
| `TrimWhitespace` | `bool` | `false` | Trims leading/trailing whitespace from cells before parsing. |
| `IgnoreBlankLines` | `bool` | `true` | Ignores consecutive or empty blank lines. |
| `ErrorMode` | `CsvErrorMode` | `FailFast` | `FailFast` (stops immediately) or `CollectAndContinue` (skips bad row). |
| `OnError` | `CsvErrorHandler` | `null` | Callback delegate triggered on any error. |
| `UseStringPooling` | `bool` | `true` | Deduplicates repetitive strings to cut memory usage. |
| `AllowCommaDecimalSeparator` | `bool` | `true` | Automatically converts comma to dot for decimal parsing. |
| `MaxFieldSize` | `int` | `16 MB` | Maximum allowable cell size (memory protection guardrail). |
| `BufferSize` | `int` | `64 KB` | Buffer size allocated for pipeline reading. |
| `DateTimeFormat` | `string?` | `null` | Default date/time format when no local `[CsvFormat]` is defined. |
| `TrueValues` / `FalseValues` | `string[]` | Common literals | Accepted textual boolean representations. |
| `NullValues` | `string[]` | `["", "null", "\\N", ...]` | Strings interpreted as `null`. |
| `IgnoreExtraColumns` | `bool` | `true` | Silently ignores extra columns beyond model properties. |
| `ColumnMapping` | `Dictionary` | `null` | Dynamic mapping dictionary: `[CSharpPropertyName] => CsvHeaderName`. |
| `DetectAndSkipBom` | `bool` | `true` | Automatically detects and skips UTF-8 BOM (`0xEF`, `0xBB`, `0xBF`). |
| `DefaultHtmlSanitizeMode` | `HtmlSanitizeMode?` | `null` | Default HTML sanitization applied to string columns (`StripTags` or `Encode`). |
| `TransformField` | `Func<string, string, string>?` | `null` | Global cell transformation: `(columnName, readValue) => modifiedValue`. |

### `CsvWriterOptions` (Writing)

| Option | Type | Default | Description |
|:---|:---|:---|:---|
| `Delimiter` | `byte` | `(byte)','` | Column separator byte during serialization. |
| `Quote` | `byte` | `(byte)'"'` | Quoting character byte. |
| `QuoteStrategy` | `QuoteStrategy` | `Minimal` | `Minimal` (only if required), `Always`, or `Never`. |
| `EscapeFormulas` | `bool` | `false` | Prefixes with `'` cells starting with `=`, `+`, `-`, `@`, `\t`, or `\r`. |
| `WriteHeader` | `bool` | `true` | Writes header row at the beginning of the file. |
| `NewLine` | `byte[]` | `\r\n` (CRLF) | Byte sequence terminating each record. |
| `FormatHeader` | `Func<string,string>` | `null` | Dynamic function to translate or reformat header labels. |
| `NullFormat` | `string` | `""` | Replacement string written when a value is null. |
| `BufferSize` | `int` | `64 KB` | Buffer size before flushing to destination stream. |
| `WriteBom` | `bool` | `false` | Emits UTF-8 BOM at file start for Microsoft Excel compatibility. |
| `TransformField` | `Func<string, string, string>?` | `null` | Global transformation applied to cells before writing: `(columnName, rawValue) => modifiedValue`. |

---

## The Ultimate Example (All-in-One)

A real-world e-commerce enterprise export combining error resilience, Native AOT, JSON serialization, and sub-object flattening:

```csharp
using System.Buffers.Text;
using System.Text.Json.Serialization;
using FlashCsv.Annotations;
using FlashCsv.Core;

namespace MyEnterpriseApp;

// 1. Flattened Sub-Objects
public partial record struct GpsCoordinates
{
    [CsvName("lat")]
    public double Latitude { get; init; }

    [CsvName("lon")]
    public double Longitude { get; init; }
}

public partial record struct ShippingDestination
{
    [CsvName("city")]
    public string City { get; init; }

    [CsvFlatten("gps_")]
    public GpsCoordinates Position { get; init; }
}

// 2. Native AOT JSON Sub-Object
public record struct CartProduct(string Sku, int Quantity);

[JsonSerializable(typeof(List<CartProduct>))]
public partial class CartContext : JsonSerializerContext { }

// 3. Zero-Allocation Condition
public readonly struct IsBusinessRule : ICsvCondition<CommercialOrder>
{
    public static bool IsSatisfied(in CommercialOrder op) => op.IsCompany;
}

// 4. Custom Binary Currency Reader
public sealed class EuroAmountParser : ICsvFieldReader<int>
{
    public static bool TryParse(ReadOnlySpan<byte> source, out int value, out CsvErrorCode errorCode)
    {
        if (source.EndsWith("€"u8)) source = source[..^3];
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

public enum DeliveryStatus
{
    [CsvEnumMemberName("PENDING", "HOLD")]
    Pending,
    [CsvEnumMemberName("SHIPPED", "DELIVERED")]
    Shipped
}

// 5. Main Root Record
[CsvSerializable]
public partial record struct CommercialOrder
{
    [CsvRowNumber]
    public int FileRow { get; init; }

    [CsvName("order_ref", "reference", "ref")]
    [CsvRequired]
    public int Id { get; init; }

    [CsvName("client_email")]
    [CsvPattern(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    [CsvSanitizeHtml(HtmlSanitizeMode.StripTags)]
    public string Email { get; init; }

    [CsvName("total_amount")]
    [CsvConverter<EuroAmountParser>]
    public int TotalAmount { get; init; }

    [CsvBoolean(["YES", "TRUE", "1"], ["NO", "FALSE", "0"])]
    public bool IsCompany { get; init; }

    [CsvRequiredIf<IsBusinessRule>]
    public string? VatId { get; init; }

    [CsvEnum(CsvEnumFormat.Name)]
    public DeliveryStatus Status { get; init; }

    [CsvDelimiterArray(delimiter: ',', escapeChar: '\\')]
    public string[] Tags { get; init; }

    [CsvJson("items_json", JsonSourceGenContext = typeof(CartContext))]
    public List<CartProduct> Items { get; init; }

    [CsvFlatten("shipping_")]
    public ShippingDestination Destination { get; init; }

    [CsvSanitizeFormula]
    public string CustomerNote { get; init; }

    [CsvExtensionData]
    public Dictionary<string, string>? ExtraColumns { get; init; }
}
```

### Execution: Robust Streaming, Ingestion & Excel Safe Export

```csharp
using FlashCsv.Core;

await using var stream = File.OpenRead("incoming_data.csv");

var readOptions = new CsvReaderOptions
{
    DetectAndSkipBom = true,
    ErrorMode = CsvErrorMode.CollectAndContinue,
    OnError = err => Console.WriteLine($"[Row {err.LineNumber}] Column '{err.ColumnName}' invalid ({err.Code}). Skipping."),
    ColumnMapping = new() { ["Id"] = "order_ref" }
};

var validOrders = new List<CommercialOrder>();
await foreach (var order in FlashCsvSerializer.DeserializeAsync<CommercialOrder>(stream, readOptions))
{
    validOrders.Add(order);
}

// Export with Excel Formula Injection Shielding
await using var exportStream = File.Create("excel_export.csv");
var writeOptions = new CsvWriterOptions
{
    WriteHeader = true,
    WriteBom = true,
    EscapeFormulas = true,
    FormatHeader = h => h.ToUpperInvariant()
};

await FlashCsvSerializer.SerializeAsync(exportStream, validOrders, writeOptions);
```

---

## License

This project is distributed under the open-source **MIT** license. You are free to use, modify, and distribute it in both personal and commercial projects.
