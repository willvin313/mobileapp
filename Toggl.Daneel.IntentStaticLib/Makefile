XBUILD=xcodebuild
PROJECT_ROOT=./
PROJECT=$(PROJECT_ROOT)/Toggl.Daneel.IntentStaticLib.xcodeproj
TARGET=Toggl.Daneel.IntentStaticLib
NAMESPACE=Toggl.Daneel.Intents

all: bind

lib$(TARGET)-i386.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphonesimulator -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphonesimulator/lib$(TARGET).a $@

lib$(TARGET)-arm64.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch arm64 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

lib$(TARGET)-armv7.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv7 -configuration Release clean build
	-mv $(PROJECT_ROOT)/build/Release-iphoneos/lib$(TARGET).a $@

lib$(TARGET).a: lib$(TARGET)-i386.a lib$(TARGET)-armv7.a lib$(TARGET)-arm64.a
	xcrun -sdk iphoneos lipo -create -output $@ $^

bind: lib$(TARGET).a
	sharpie bind -scope Toggl.Daneel.IntentStaticLib -output bo -namespace $(NAMESPACE) -sdk iphoneos12.0 Toggl.Daneel.IntentStaticLib/*.h

clean:
	-rm -rf bo build *.a *.dll
