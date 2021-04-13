# AWSome Blog Post - Part II (the Back-End)

This was a prototype for an internal onboarding program - PoC/demo session.

This repository is the Solution's Microservice for API/Back-end.

The repository for the front-end can be found [here]( https://github.com/aws-samples/video-labeling-angular-blog ).

Created by M. França
Mentored by J. Baeta

## Setup

### [AWS Organizations]( https://aws.amazon.com/organizations/ )
### [Manage SSO to your AWS accounts]( https://docs.aws.amazon.com/singlesignon/latest/userguide/manage-your-accounts.html )
### [Configuring the AWS CLI to use AWS Single Sign-On]( https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sso.html )
### [Named Profiles]( https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-profiles.html )
### [SAM]( https://aws.amazon.com/serverless/sam/ )

~/.aws/credentials (Linux & Mac) or %USERPROFILE%\.aws\credentials (Windows) 

Example:
```
[default]
aws_access_key_id = XYZEXAMPLE1
aws_secret_access_key = blaBlaBlaEXAMPLEKEY
```

Each profile can specify different credentials—perhaps from different IAM users—and can also specify different AWS Regions and output formats.

~/.aws/config (Linux & Mac) or %USERPROFILE%\.aws\config (Windows) 

Considering $> aws configure sso...
```
[default]
region = us-east-2
output = json
[profile tenant1]
sso_start_url = https://mysaas.awsapps.com/start
sso_region = us-east-2
sso_account_id = 1234567891
sso_role_name = AdministratorAccess
region = us-east-2
output = json
[profile tenant2]
sso_start_url = https://mysaas.awsapps.com/start
sso_region = us-east-2
sso_account_id = 1234567892
sso_role_name = AdministratorAccess
region = us-east-2
output = json
```

## Provisioning

Considering $> git clone...

```
[aws sso login --profile tenant1 | tenant2]
sam validate
sam build
sam deploy --guided --profile tenant1 | tenant2
```

### Web API (S3 URLs) Microservice
* Stack name: fra-awsomeblog-back-stk
* AWS Region: us-east-2
* pTenantName: AWSomeTV | AWSomeTV2
* pProjectName: prjAWSomeBlog
* pCallbackURL: http://localhost:4200/login,https://\<domain\>.cloudfront.net/login
* pLogoutURL: http://localhost:4200/home,https://\<domain\>.cloudfront.net/home
* pAdminEmail: your@email.here
* pTenantId: 1 | 2
* SAM configuration environment: tenant1 | tenant2

### Video Ingestion Microservice
* on the next blog post/repository

### Start Label Detection Microservice
* on the next blog post/repository

### Get Label Detection Microservice
* on the next blog post/repository

## Operation

### Watching the Logs

```
sam logs -n MyAWSomeLambda --stack-name <stack-name> --tail --profile tenant1 | tenant2
```

### Decomissioning
```
aws cloudformation delete-stack --stack-name <stack-name> --region us-east-2 --profile tenant1 | tenant2
```

## Contact
M. França - http://www.linkedin.com/in/mafranca