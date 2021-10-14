# README #

## Aqovia.DurableFunctions.Testing

Nuget Info

[![NuGet Info](https://buildstats.info/nuget/Aqovia.DurableFunctions.Testing?includePreReleases=true)](https://www.nuget.org/packages/Aqovia.DurableFunctions.Testing/)

Build status

[![.NET Core](https://github.com/Aqovia/durable-functions-test/workflows/Nuget%20Publish%20CI/badge.svg?branch=master)](https://github.com/Aqovia/durable-functions-test/actions?query=branch%3Amaster)

[![Windows Build history](https://buildstats.info/github/chart/Aqovia/durable-functions-test?branch=master&includeBuildsFromPullRequest=false)](https://github.com/Aqovia/durable-functions-test/actions?query=branch%3Amaster)

## About

Aqovia.DurableFunctions.Testing provides functionality to test a durable functions end-to-end using standard test frameworks. It allows the possibility to remove mocks from the testing process and utilise the real durable task extensions during execution of the test. It is not intended to replace standard unit tests as full code execution coverage is not possible due to external/third party tool requirements. It can be used for testing the integration of azure functions with orchestrations and any related activity function/sub-orchestrations for success/happy path scenarios. This test approach allows the testing of the azure function stack (with external components) to be run as part of a CI/CD pipeline.

## Getting started

* Install and start Azure Storage Emulator (or Azurite)
* Include the Nuget package in your test project
* Include the durable task libs in your project


## Project overview

The project contains the following directory structure

```
src/
    DurableFunctions.Test
test/
    DurableFunctions.Test.EndToEndTests
    SampleFunctionApp
```

### src/DurableFunctions.Test

Contains the source library code for creating an in-process job host. A wrapper is provided to configure this job host as the per the function app which is under test by providing fake service instances to the DI container.

### test/SampleFunctionApp

Contains a basic function app (created from visual studio template) - which can be used as a test app for demonstrating/testing the test library.

### test/DurableFunctions.Test.EndToEndTests

Contains sample tests for demonstrating the usage of the test library. 

Tests include:
1. Http triggered function -> Orchestration function -> Activity function
2. Service Bus triggered function -> Orchestration function -> Activity function (Todo)
3. Service Bus triggered function -> Orchestration function -> Http Client (Todo)
4. Queue triggered function -> Orchestration function -> Sub-Orchestration -> Activity Function (Todo)

## Contributing

Assuming the repository is cloned and up-to-date (`develop` branch). Assume using approximation to git flow branch naming convention.

1. Create a branch from `master` branch using `git checkout -b feature/new_feature`
2. Implement changes on new feature branch
3. Test and build locally - updating tests if required
4. Push to remote and fix any remote build/test issues
5. Create a pull request to the `master` branch
  - once all checks and reviews are passed you will be able to merge the branch via squash-commit merging
  - at this point you should create a summary ofthe new feature or fix in the provided commit message ui fields
    - if this merge implements a new fix include the string 'bump: patch'
	- if this merge implements a new feature include the string 'bump: minor'
	- if this merge implements a new feature with breaking changes include the string 'bump: major'
    
## Release Process

- The release process is automated by the CI process for every successful merge to master.
- The PR request title and description are used to create the Release note found via the `Releases` link on the repo landing page
- Inclusion of the keywords (bump: major|minor|patch) in the PR description is sufficient for the developer to control the upgrade to the final semantic version of the package
- Branch Preview packages are also available via the Aqovia Nuget OSS Feeds (publically available)
- Release packages are available on the [Nuget.org](https://www.nuget.org/packages/Aqovia.DurableFunctions.Testing) 
- Githib release info is also available to view/compare and download source via the repo landing page

