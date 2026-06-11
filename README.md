This document is a guided lab instruction sheet for a backend software development module focused on ASP.NET Core 10 Fundamentals. It outlines a practical hands-on session where you will fix and improve the request pipeline of a Training Management System (TMS) API.

Here is a brief, line-by-line explanation of every detail in the text:

Module Header & Metadata
"Module 4 Guided Lab Session 1: Request Flow & Visibility": This is the first lab session of Module 4, focusing on how HTTP requests travel through the application and how to make those operations visible via logging.

"Module | M4 ASP.NET Core 10 Fundamentals": Identifies the course curriculum area as ASP.NET Core 10 basics.

"Exercises | 1 (Middleware Ordering), 1B (Custom Request Logging Middleware)": Lists the two core exercises you will complete during this lab.

"After this session you can show...": Defines the grading or success criteria. You must be able to demonstrate three things: unauthenticated users are blocked, responses return a tracking ID, and logs match up perfectly using that ID.

The Goal of the Session
"Welcome to the Backend Foundation Sprint": Introduces this phase of your backend development training.

"You keep building the same Training Management System (TMS) API you will extend it": You aren't starting from scratch; you are continuing to build on a recurring project called the Training Management System.

"A previous build’s Program.cs runs, but behaviour on a sensitive route is wrong...": The application compiles and starts up, but a critical security flaw exists on a private URL. You will find and fix it by analyzing the HTTP response codes and the order of operations.

"Later you add tracing so failures are tied to a single request": After fixing the bug, you will implement a tracking system so system errors can be traced back to the exact user request that caused them.

"In M1 you modelled Student, Course... In M4 you host those concerns on the web server": Reminds you that in Module 1, you created the data structures (Classes/Models). Now, in Module 4, you are making them accessible over the internet via a web server.

"Session 1 is narrow on purpose...": Clarifies that this lab focuses deeply on just two things: securing a private endpoint and setting up robust debugging logs.

Breakdown of the Two Exercises
"By the end of Exercise 1: GET /api/assessments/results rejects anonymous callers with 401...": The ultimate goal of the first exercise is to ensure that trying to read assessment grades without logging in results in an HTTP 401 Unauthorized error instead of leaking data.

"You earn that outcome by fixing how the pipeline is wired—without relying on comments...": You will achieve this security fix by understanding how middleware ordering works, rather than looking for cheat-sheet comments in the code.

"By the end of Exercise 1B: you wrap the pipeline in custom logging...": In the second exercise, you will inject code to intercept all traffic, generate a unique X-Correlation-Id header, and set up a global error-handling safety net (UseExceptionHandler).

"...the same posture you will reconnect when Session 3 adds ProblemDetails and Scalar": This sets up the foundational architecture that future labs (Session 3) will plug into for standard API error formatting and API documentation.

Setup Instructions: The TmsApi Project
"Before you begin: the TmsApi project": Prepares you to set up your environment.

"Module 4 uses one ASP.NET Core Web API project... Create it once... and keep working in that same folder": A strict warning to create this folder exactly once. Future exercises build on top of this exact codebase, so you shouldn't create a new project for later labs.

"If you closed your machine... open the existing TmsApi directory... do not run dotnet new again": A reminder to reuse your existing directory if you take a break and come back later.

Step-by-Step Terminal Commands
"Open your terminal and run these commands one time when you begin M4": Directs you to open your command line interface to execute the initial configuration.

dotnet --version (Check your SDK version: must show 10.x): Verifies that your computer is running the correct, up-to-date .NET 10 Software Development Kit.

dotnet new webapi -n TmsApi --no-openapi --use-controllers: Creates a new Web API project named TmsApi using traditional Controllers rather than Minimal APIs, while skipping the default documentation setup for now.

cd TmsApi (Move into the project directory): Navigates your terminal inside the newly created project folder.

code . (Open it in VS Code): Launches Visual Studio Code directly inside your project workspace.

"Find Program.cs. You will rewrite it throughout this session": Points you to the main entry file of the application, which is where your middleware pipeline edits will happen.

dotnet run (Confirm the project builds and runs): Compiles the source code and starts the local web server.

info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5xxx: Shows you what a successful terminal startup message looks like, confirming your backend is live on a local port.

"Press Ctrl+C to stop the server": Explains how to safely shut down the running backend application in your terminal.
