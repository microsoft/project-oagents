
# Make environment variables out of all outputs from bicep file
Write-Verbose "---Setting environment variables..." -Verbose
  azd env get-values | % {
    $name,$value = $_.Split('=')

    #Value is quoted, so remove quotes
    $value = $value.Replace('"','')
    
    [System.Environment]::SetEnvironmentVariable($name,$value)
    Write-Verbose "Variable '$name' set to '$value'" -Verbose
  }
Write-Verbose "---Done setting environment variables" -Verbose

$templateFileName = ".env.template"
$prodEnvFileName = ".env"

# Replace the backend endpoint in the frontend .env file
Write-Verbose "---Updating '$prodEnvFileName' file updated with backend endpoint..." -Verbose
  pushd src/frontend
  if (-not (Test-Path -Path $templateFileName)) {
    Write-Error "The file '$templateFileName' does not exist."
  }
  Copy-Item -Path $templateFileName -Destination $prodEnvFileName
  (Get-Content $prodEnvFileName) -replace '<AZURE_BACKEND_URI>', $env:AZURE_BACKEND_URI | Set-Content $prodEnvFileName
  popd
Write-Verbose "---Done updating '$prodEnvFileName' file updated with backend endpoint..." -Verbose
