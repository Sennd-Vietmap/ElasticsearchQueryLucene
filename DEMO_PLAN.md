# Demo Project Plan: Lucenet's Pet Shelter

## 🎯 Objective
Create a console application to demonstrate the **Full CRUD** and **Search** capabilities of the `ElasticsearchQueryLucene` EF Core Provider. The project will simulate a Pet Shelter management system where users can add pets, update their status, delete records, and perform full-text searches on pet descriptions.

## 🛠 Technology Stack
*   **Project Type**: .NET 8.0 Console Application
*   **ORM**: Entity Framework Core 10.0
*   **Provider**: `ElasticsearchQueryLucene.EntityFrameworkCore` (Local Reference)
*   **Storage**: Lucene.Net 4.8 (FileSystemDirectory or RAMDirectory)

## 📦 Data Model
**Entity**: `Pet`
*   `int Id` (Primary Key)
*   `string Name` (Stored, Tokenized)
*   `string Breed` (Stored, Not Tokenized - for exact filtering)
*   `string Description` (Stored, Tokenized, StandardAnalyzer - for full-text search)
*   `int Age` (Stored, Unindexed)
*   `bool IsAdopted` (Stored)

## 📋 Implementation Steps

### 1. Project Setup
- Create new console app: `ElasticsearchQueryLucene.Demo`
- Add project reference to `ElasticsearchQueryLucene.EntityFrameworkCore`
- Add `Microsoft.EntityFrameworkCore.Design` (optional)

### 2. Database Context
- Implement `PetContext : DbContext`
- Configure `OnConfiguring` to use `UseLucene()`
- Configure entity mappings using Data Annotations (`[LuceneField]`)

### 3. Workflow Implementation (Program.cs)
The application will execute the following lifecycle:

#### A. Data Seeding (Create)
- Initialize the Lucene index.
- Add a collection of distinct pets (e.g., "Golden Retriever", "Siamese Cat", "Parrot").
- Call `SaveChanges()` to commit to Lucene.

#### B. Full-Text Search (Read)
- Demonstrate **LINQ Search**: `Where(p => p.Description.Contains("friendly"))`
- Demonstrate **Exact Match**: `Where(p => p.Breed == "Golden Retriever")`
- Demonstrate **Sorting**: `OrderBy(p => p.Age)`
- Demonstrate **Pagination**: `Skip(0).Take(5)`

#### C. Advanced Search (Phase 7 Features)
- Use `EF.Functions.LuceneMatch` for fuzzy search (e.g., searching for "Retreiver~" handling typos).
- Use Boolean operators in raw queries.

#### D. Update Operation
- Retrieve a specific pet.
- Modify property (e.g., change `IsAdopted` to true, update `Description`).
- Call `SaveChanges()` (Verify `UpdateDocument` logic).

#### E. Delete Operation
- remove a pet from the context.
- Call `SaveChanges()` (Verify `DeleteDocuments` logic).
- Confirm deletion by attempting to retrieve the pet.

## 🚀 Execution Strategy
1.  Run `dotnet new console -n ElasticsearchQueryLucene.Demo`
2.  Link references.
3.  Implement `Pet.cs` and `PetContext.cs`.
4.  Write logic in `Program.cs`.
5.  Run and verify output.
