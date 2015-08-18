#!/bin/sh
export DYLIB=liblabjackusb-2.5.2.bundle
mkdir "$TARGET_BUILD_DIR/$TARGET_NAME.bundle/Contents/Frameworks"
cp -f "$SRCROOT/$DYLIB" "$TARGET_BUILD_DIR/$TARGET_NAME.bundle/Contents/Frameworks"
install_name_tool -change /usr/local/lib/$DYLIB @loader_path/../Frameworks/$DYLIB "$TARGET_BUILD_DIR/$TARGET_NAME.bundle/Contents/MacOS/$PRODUCT_NAME"
