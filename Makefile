CONFIGURATION ?= Release

ifneq ($(filter $(CONFIGURATION),Debug Release),$(CONFIGURATION))
$(error CONFIGURATION must be Debug or Release)
endif

BUILD_PROJECT := eng/Build.proj

.DEFAULT_GOAL := all

.PHONY: all validate tool-restore restore format build test audit outdated sbom help

all:
	@dotnet msbuild "$(BUILD_PROJECT)" -property:Configuration=$(CONFIGURATION)

validate:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Validate -property:Configuration=$(CONFIGURATION)

tool-restore:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:ToolRestore -property:Configuration=$(CONFIGURATION)

restore:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Restore -property:Configuration=$(CONFIGURATION)

format:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Format -property:Configuration=$(CONFIGURATION)

build:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Build -property:Configuration=$(CONFIGURATION)

test:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Test -property:Configuration=$(CONFIGURATION)

audit:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Audit -property:Configuration=$(CONFIGURATION)

outdated:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Outdated -property:Configuration=$(CONFIGURATION)

sbom:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Sbom -property:Configuration=$(CONFIGURATION)

help:
	@echo "Usage: make [all|validate|tool-restore|restore|format|build|test|audit|outdated|sbom] [CONFIGURATION=Debug|Release]"
