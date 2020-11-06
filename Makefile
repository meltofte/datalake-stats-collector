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

check_vars:
ifeq (${TAG},)
	$(error TAG is not set; use "make [target] TAG=v1" with an appropriate version)
endif
