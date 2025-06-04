
To run this project you need to create a `appsettings.json` file 

It file should contain the following sections:

{
  "ConnectionStrings": {
    "UniversityDatabase": "YourConnectionStringHere"
  },
  "Jwt": {
    "Audience": "http://localhost:5300",
    "Issuer": "http://localhost:5300",
    "Key": "YourJwtKey",
    "ValidInMinutes": 10
  }
}

