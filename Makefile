CONFIGURATION ?= Release
MARKDOWNLINT ?= markdownlint
MARKDOWNLINT_VERSION := 0.49.1

ifneq ($(filter $(CONFIGURATION),Debug Release),$(CONFIGURATION))
$(error CONFIGURATION must be Debug or Release)
endif

BUILD_PROJECT := eng/Build.proj

.DEFAULT_GOAL := all

.PHONY: all validate tool-restore restore format docs-lint build test pack audit outdated sbom ci help

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

docs-lint:
	@command -v "$(MARKDOWNLINT)" >/dev/null 2>&1 || { \
		echo "markdownlint-cli $(MARKDOWNLINT_VERSION) is required."; exit 127; }
	@test "$$($(MARKDOWNLINT) --version)" = "$(MARKDOWNLINT_VERSION)" || { \
		echo "markdownlint-cli $(MARKDOWNLINT_VERSION) is required."; exit 1; }
	@$(MARKDOWNLINT) -c .markdownlint.json README.md 'docs/**/*.md' \
		src/IX.Modularity.Analyzers/README.md \
		'src/IX.Modularity.Analyzers/docs/**/*.md'

build:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Build -property:Configuration=$(CONFIGURATION)

test:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Test -property:Configuration=$(CONFIGURATION)

pack:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Pack -property:Configuration=$(CONFIGURATION)

audit:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Audit -property:Configuration=$(CONFIGURATION)

outdated:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Outdated -property:Configuration=$(CONFIGURATION)

sbom:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Sbom -property:Configuration=$(CONFIGURATION)

ci:
	@dotnet msbuild "$(BUILD_PROJECT)" -target:Ci -property:Configuration=$(CONFIGURATION) -property:CI=true

help:
	@echo "Usage: make [all|validate|tool-restore|restore|format|docs-lint|build|test|pack|audit|outdated|sbom|ci] [CONFIGURATION=Debug|Release]"
