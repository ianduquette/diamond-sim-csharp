# Create file
mkdir diamond-sim-csharp
cd diamond-sim-csharp

# create solution
dotnet new sln -n DiamondSim

# main library
dotnet new classlib -n DiamondSim -o src/DiamondSim
dotnet sln add src/DiamondSim/DiamondSim.csproj

# test project using NUnit
dotnet new nunit -n DiamondSim.Tests -o tests/DiamondSim.Tests
dotnet sln add tests/DiamondSim.Tests/DiamondSim.Tests.csproj

# add reference to the library
dotnet add tests/DiamondSim.Tests/DiamondSim.Tests.csproj reference src/DiamondSim/DiamondSim.csproj

# sanity check
dotnet build
dotnet test
