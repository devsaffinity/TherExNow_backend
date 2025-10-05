terraform {
  required_providers {
    azuredevops = {
      source = "microsoft/azuredevops"
      version = ">=0.1.0"
    }
  }
}

provider "azuredevops" {
  personal_access_token = "REDACTED"
  org_service_url = "https://dev.azure.com/apsymodels"
}

resource "azuredevops_project" "project" {
  name = "HepNow"
  description = ""
  visibility = "private"
  version_control = "git"
  work_item_template = "Agile"
}

resource "azuredevops_git_repository" "project" {
  project_id     = azuredevops_project.project.id
  name           = "HepNow.Api"
  default_branch = "refs/heads/main"
  initialization {
    init_type = "Clean"
  }
  lifecycle {
    ignore_changes = [
      initialization,
    ]
  }
}

resource "azuredevops_build_definition" "project" {
  project_id = azuredevops_project.project.id
  name       = "HepNow Build Pipeline"

  ci_trigger {
    use_yaml = true
  }

  repository {
    repo_type = "TfsGit"
    repo_id   = azuredevops_git_repository.project.id
    yml_path  = "azure-pipelines.yml"
  }
}