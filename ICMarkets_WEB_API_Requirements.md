# ICMarkets WEB API .NET Developer Project

Greetings from ICMarkets team.

The purpose of this project is to validate the candidate's approach to designing a Web API. The application stores data from the following ETH, Dash, BTC, and LTC APIs in a database.

## Data Sources — BlockCypher

1. Ethereum Mainnet: https://api.blockcypher.com/v1/eth/main
2. Dash Mainnet: https://api.blockcypher.com/v1/dash/main
3. Bitcoin Mainnet: https://api.blockcypher.com/v1/btc/main
4. Bitcoin Testnet3: https://api.blockcypher.com/v1/btc/test3
5. Litecoin Mainnet: https://api.blockcypher.com/v1/ltc/main

Documentation: [BlockCypher Blockchain API](https://www.blockcypher.com/dev/bitcoin/)

## Minimum Functionality

1. Build a .NET Core application using **Clean Architecture** or **Vertical Slice Architecture**, following **SOLID** principles.
2. Expose HTTP API endpoints documented in **Swagger**. Endpoints must provide the history of stored data for each blockchain.
3. Save blockchain data in the database with an additional **`CreatedAt`** timestamp indicating when the data was requested from the external API. Return history sorted by **`CreatedAt` descending**.
4. Provide a **HealthChecks** endpoint and a basic **CORS** policy.
5. Implement **dependency injection, logging, model mapping, API serialization, and automatic model validation**.
6. Include **unit, integration, and functional test projects**.
7. Support runtime profiles for **.NET** and **Docker (Linux)**.

## Framework and Design Requirements

1. Use **.NET Core**, preferably **.NET 6 or later**.
2. Choose a database: **SQLite with EF**, a database running in **Docker**, or **NoSQL**.
3. Store the main data as provided in the external APIs' **JSON responses**.
4. Demonstrate best practices for **performance, inheritance, and scalability**.
5. Use **asynchronous or parallel patterns**, such as Tasks or PLINQ.
6. Implement **at least two design patterns** from: **Unit of Work, Repository, CQRS, Event Sourcing**.
7. **Optional:** API Gateway.

## Submission

- Submit the solution by email with a **public GitHub repository URL**.
- Include a **`README.md`** with setup and run instructions.
- The repository must have **`main`** and **`development`** branches.

---

*This Markdown file reformats the provided ICMarkets assignment. The original document does not explicitly require scheduled background collection; that would be an implementation choice.*
