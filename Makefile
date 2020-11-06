.PHONY: check_vars build push pull test deploy

IMAGE := cfbregistry.azurecr.io/datalake_stats
NAME := datalake-stats
RESOURCE_GROUP := rg-dl-dev
#TAG := $(shell git log --pretty=format:'%h' -n 1)
TAG := v1

# default target first
build: check_vars
	docker build . --tag ${IMAGE}:${TAG}

push: check_vars
	az acr login --name cfbregistry
	docker push ${IMAGE}:${TAG}

pull: check_vars
	az acr login --name cfbregistry
	docker pull ${IMAGE}:${TAG}

test: check_vars
	docker run --rm --name datalakestats --env-file Docker.env ${IMAGE}:${TAG}

delete-container:
	az container delete --name ${NAME} --resource-group ${RESOURCE_GROUP}

create: check_vars
	az container create  \
	--resource-group ${RESOURCE_GROUP} \
	--name ${NAME} \
	--image ${IMAGE}:${TAG} \
	--location northeurope \
	--cpu 1 \
	--memory 0.5 \
	--restart-policy Never \
	--environment-variables 'AZURE_TENANT_ID'='f251f123-c9ce-448e-9277-34bb285911d9' 'AZURE_CLIENT_ID'='91f4882a-3c1c-4579-9ecc-6bf2250e109a' 'NGS_ACCOUNT'='cfbngsv2' 'NGS_SAMPLES_CONTAINER'='samples' 'NGS_NEXTSEQ_CONTAINER'='nextseq01' 'NGS_MISEQ_CONTAINER'='miseq01' 'PROTEOMICS_ACCOUNT'='cfbproteomics' 'PROTEOMICS_CONTAINER'='proteomics' 'DWH_SERVER_NAME'='cfb-dev' 'DWH_DB_NAME'='dwh' 'DWH_USER_NAME'='cfb_dwh_etl_user' \
	--secure-environment-variables 'AZURE_CLIENT_SECRET'='79VlxF_-Ddx5gTY~H.-7jr6IW.wRlKZhN6' 'DWH_PASSWORD'='nV8UKU8pwFAYmEZpDiaEPVgwhkVoWG5D/65kXiguIxQ=' \
	--registry-login-server cfbregistry.azurecr.io \
	--registry-username cfbregistry \
	--registry-password q/mZ8g1kdgurxEKDvHN6FNCbllWNPhxJ

create2: check_vars
	az container create  \
	--resource-group ${RESOURCE_GROUP} \
	--name ${NAME} \
	--image ${IMAGE}:${TAG} \
	--location northeurope \
	--cpu 1 \
	--memory 0.5 \
	--restart-policy Never \
	--environment-variables 'NGS_ACCOUNT'='cfbngsv2' 'NGS_SAMPLES_CONTAINER'='samples' 'NGS_NEXTSEQ_CONTAINER'='nextseq01' 'NGS_MISEQ_CONTAINER'='miseq01' 'PROTEOMICS_ACCOUNT'='cfbproteomics' 'PROTEOMICS_CONTAINER'='proteomics' 'DWH_SERVER_NAME'='cfb-dev' 'DWH_DB_NAME'='dwh' 'DWH_USER_NAME'='cfb_dwh_etl_user' \
	--secure-environment-variables 'DWH_PASSWORD'='nV8UKU8pwFAYmEZpDiaEPVgwhkVoWG5D/65kXiguIxQ=' \
	--registry-login-server cfbregistry.azurecr.io \
	--registry-username cfbregistry \
	--registry-password q/mZ8g1kdgurxEKDvHN6FNCbllWNPhxJ \
	--assign-identity /subscriptions/aee8556f-d2fd-4efd-a6bd-f341a90fa76e/resourceGroups/rg-dl-dev/providers/Microsoft.ManagedIdentity/userAssignedIdentities/datalake-stats-collector-identity

run: check_vars
	az container start --name ${NAME} --resource-group ${RESOURCE_GROUP}

check_vars:
ifeq (${TAG},)
	$(error TAG is not set; use "make [target] TAG=v1" with an appropriate version)
endif
