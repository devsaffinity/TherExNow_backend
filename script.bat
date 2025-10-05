ECHO Start building solution
dotnet build

ECHO Initialize databae migration
cd %1
dotnet ef migrations add initMigration

Echo Creating Database
cd %1
dotnet ef database update

ECHO Generate Azure DevOps project and repository
terraform init
terraform apply -auto-approve
PAUSE

ECHO Init git
git init

ECHO commit changes
git add .
git commit -m "Initial Commit"

ECHO push changes
git push --set-upstream "git remote add origin git@ssh.dev.azure.com:v3/apsymodels/%2/%2.Api" main

