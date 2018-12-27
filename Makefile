.PHONY: all clean

PROJECT=SmartTank

SOURCEDIR=src
SOURCE=$(wildcard $(SOURCEDIR)/*.cs) $(wildcard $(SOURCEDIR)/*/*.cs) $(wildcard $(SOURCEDIR)/*.csproj)
ASSETDIR=assets
ICONS=$(wildcard $(ASSETDIR)/*.png)
CONFIGS=$(wildcard $(ASSETDIR)/*.cfg) $(wildcard $(ASSETDIR)/*.ltp)
LANGUAGES=$(ASSETDIR)/lang
README=README.md
GAMELINK=$(SOURCEDIR)/KSP_Data
PROCPARTSLINK=$(SOURCEDIR)/ProceduralParts
DEFAULTGAMEDIR=$(HOME)/.local/share/Steam/steamapps/common/Kerbal Space Program

DEBUGDLL=$(SOURCEDIR)/bin/Debug/$(PROJECT).dll
RELEASEDLL=$(SOURCEDIR)/bin/Release/$(PROJECT).dll
DISTDIR=$(PROJECT)
RELEASEZIP=$(PROJECT).zip
DLLDOCS=$(SOURCEDIR)/bin/Release/$(PROJECT).xml
DLLSYMBOLS=$(SOURCEDIR)/bin/Debug/$(PROJECT).pdb
LICENSE=LICENSE
INTERNALCKAN=$(PROJECT).ckan
VERSION=$(PROJECT).version
TAGS=tags

TARGETS=$(DEBUGDLL) $(RELEASEDLL) $(RELEASEZIP)

all: $(TAGS) $(TARGETS)

$(TAGS): $(SOURCE)
	ctags -f $@ $^

$(DLLSYMBOLS): $(DEBUGDLL)

$(DLLDOCS): $(RELEASEDLL)

$(DEBUGDLL): $(SOURCE) $(GAMELINK) $(PROCPARTSLINK)
	cd $(SOURCEDIR) && msbuild /p:Configuration=Debug

$(RELEASEDLL): $(SOURCE) $(GAMELINK) $(PROCPARTSLINK)
	cd $(SOURCEDIR) && msbuild /p:Configuration=Release

$(RELEASEZIP): $(RELEASEDLL) $(ICONS) $(README) $(LICENSE) $(INTERNALCKAN) $(VERSION) $(CONFIGS) $(LANGUAGES)
	mkdir -p $(DISTDIR)
	cp -a $^ $(DISTDIR)
	zip -qr $@ $(DISTDIR) -x \*.settings

$(PROCPARTSLINK):
	if [ ! -x "$(DEFAULTGAMEDIR)" ]; \
	then \
		echo "$(PROCPARTSLINK) not found."; \
		echo 'This must be a symlink to the folder where ProceduralParts is installed.'; \
		exit 2; \
	elif [ ! -x "$(DEFAULTGAMEDIR)/GameData/ProceduralParts" ]; \
	then \
		echo "ProceduralParts is not installed at $(DEFAULTGAMEDIR)." \
		echo "It is a prerequisite for this mod." \
		exit 2; \
	else \
		ln -s "$(DEFAULTGAMEDIR)"/GameData/ProceduralParts $(PROCPARTSLINK); \
	fi

$(GAMELINK):
	if [ -x "$(DEFAULTGAMEDIR)" ]; \
	then \
		ln -s "$(DEFAULTGAMEDIR)"/KSP_Data $(GAMELINK); \
	else \
		echo "$(GAMELINK) not found."; \
		echo 'This must be a symlink to Kerbal Space Program/KSP_Data.'; \
		exit 2; \
	fi

clean:
	cd $(SOURCEDIR) && msbuild /t:Clean
	rm -f $(TARGETS) $(TAGS)
	rm -rf $(SOURCEDIR)/bin $(SOURCEDIR)/obj $(DISTDIR)
