If Android build files are missing on your machine (Gradle wrapper, etc.):

1) Install Flutter SDK
2) In `mobile/online_store_mobile` run:
   flutter create .
This will regenerate the full android folder scaffold.

Then keep:
- lib/
- pubspec.yaml

(You can overwrite files if needed.)
