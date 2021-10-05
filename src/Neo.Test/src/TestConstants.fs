module Neo.Test.TestConstants

open Bogus
open Neo4j.Driver

let driver =
    GraphDatabase.Driver ("bolt://localhost:7687", AuthTokens.Basic ("neo4j", "password"))
let faker = Faker ()
