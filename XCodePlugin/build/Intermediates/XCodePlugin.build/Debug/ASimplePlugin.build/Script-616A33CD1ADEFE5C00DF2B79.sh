#!/bin/sh
export BUNDLE=liblabjackusb.bundle

mkdir "$TARGET_BUILD_DIR/$TARGET_NAME.bundle/Contents/Frameworks"

cp -f "$SRCROOT/$BUNDLE" "$TARGET_BUILD_DIR/$TARGET_NAME.bundle/Contents/Frameworks"

install_name_tool -change /usr/local/lib/$BUNDLE @loader_path/../Frameworks/$BUNDLE "$TARGET_BUILD_DIR/$TARGET_NAME.bundle/Contents/MacOS/$PRODUCT_NAME"
