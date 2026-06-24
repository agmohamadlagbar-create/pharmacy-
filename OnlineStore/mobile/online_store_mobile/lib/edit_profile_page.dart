import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';

class EditProfilePage extends StatefulWidget {
  const EditProfilePage({super.key});

  @override
  State<EditProfilePage> createState() => _EditProfilePageState();
}

class _EditProfilePageState extends State<EditProfilePage> {
  final _formKey = GlobalKey<FormState>();

  final _nameCtrl = TextEditingController();
  final _countryCtrl = TextEditingController();
  final _streetCtrl = TextEditingController();
  final _buildingCtrl = TextEditingController();
  final _cityCtrl = TextEditingController();
  final _postalCtrl = TextEditingController();
  
  final _descriptionCtrl = TextEditingController();
  final _openingTimeCtrl = TextEditingController();
  final _closingTimeCtrl = TextEditingController();
  String _accountType = '';

  LatLng _selectedLocation =
      const LatLng(41.0082, 28.9784); // Default to Istanbul
  bool _isLoading = true;
  bool _isSaving = false;
  final MapController _mapController = MapController();

  @override
  void initState() {
    super.initState();
    _fetchProfile();
  }

  Future<void> _fetchProfile() async {
    try {
      final res = await Api.get('/api/me');
      setState(() {
        _nameCtrl.text = res['fullName']?.toString() ?? '';
        _countryCtrl.text = res['country']?.toString() ?? '';
        _streetCtrl.text = res['street']?.toString() ?? '';
        _buildingCtrl.text = res['buildingNumber']?.toString() ?? '';
        _cityCtrl.text = res['city']?.toString() ?? '';
        _postalCtrl.text = res['postalCode']?.toString() ?? '';
        _accountType = res['accountType']?.toString() ?? '';
        _descriptionCtrl.text = res['description']?.toString() ?? '';
        _openingTimeCtrl.text = res['openingTime']?.toString() ?? '';
        _closingTimeCtrl.text = res['closingTime']?.toString() ?? '';

        if (res['latitude'] != null && res['longitude'] != null) {
          _selectedLocation = LatLng(
            (res['latitude'] as num).toDouble(),
            (res['longitude'] as num).toDouble(),
          );
        }
        _isLoading = false;
      });
      // Small delay to ensure map is ready
      Future.delayed(const Duration(milliseconds: 500), () {
        _mapController.move(_selectedLocation, 14);
      });
    } catch (e) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _onMapTap(TapPosition tapPos, LatLng position) async {
    setState(() {
      _selectedLocation = position;
    });
    _mapController.move(position, _mapController.camera.zoom);

    // Reverse geocoding using Nominatim (free OSM service)
    try {
      final url = Uri.parse(
          'https://nominatim.openstreetmap.org/reverse?format=json&lat=${position.latitude}&lon=${position.longitude}&zoom=18&addressdetails=1');
      final response =
          await http.get(url, headers: {'User-Agent': 'OnlineStoreMobileApp'});

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final addr = data['address'];
        if (addr != null) {
          setState(() {
            _streetCtrl.text = addr['road'] ?? addr['suburb'] ?? '';
            _cityCtrl.text = addr['city'] ??
                addr['town'] ??
                addr['village'] ??
                addr['state'] ??
                '';
            _postalCtrl.text = addr['postcode'] ?? '';
            _countryCtrl.text = addr['country'] ?? _countryCtrl.text;
          });
        }
      }
    } catch (e) {
      // ignore geocoding errors
    }
  }

  Future<void> _getCurrentLocation() async {
    bool serviceEnabled;
    LocationPermission permission;

    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) return;

    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) return;
    }

    if (permission == LocationPermission.deniedForever) return;

    final pos = await Geolocator.getCurrentPosition();
    final latLng = LatLng(pos.latitude, pos.longitude);
    _onMapTap(const TapPosition(Offset(0, 0), Offset(0, 0)), latLng);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSaving = true);
    try {
      await Api.put('/api/me/profile', {
        'fullName': _nameCtrl.text,
        'country': _countryCtrl.text,
        'street': _streetCtrl.text,
        'buildingNumber': _buildingCtrl.text,
        'city': _cityCtrl.text,
        'postalCode': _postalCtrl.text,
        'latitude': _selectedLocation.latitude,
        'longitude': _selectedLocation.longitude,
        if (_descriptionCtrl.text.isNotEmpty) 'description': _descriptionCtrl.text,
        if (_openingTimeCtrl.text.isNotEmpty) 'openingTime': _openingTimeCtrl.text,
        if (_closingTimeCtrl.text.isNotEmpty) 'closingTime': _closingTimeCtrl.text,
      });
      if (mounted) {
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isSaving = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${'Failed to save profile:'.tr()} $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    return Scaffold(
      appBar: AppBar(
        title: Text('Edit Profile'.tr()),
        actions: [
          if (_isSaving)
            const Padding(
              padding: EdgeInsets.all(16.0),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2)),
            )
          else
            IconButton(icon: const Icon(Icons.check), onPressed: _save),
        ],
      ),
      body: Form(
        key: _formKey,
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Personal Information'.tr(),
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
              const SizedBox(height: 12),
              TextFormField(
                controller: _nameCtrl,
                decoration: InputDecoration(
                  labelText: _accountType == 'Shop' ? 'Shop Name'.tr() : 'Full Name'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                  prefixIcon: const Icon(Icons.person_outline),
                ),
                validator: (v) => v!.isEmpty ? 'This field is required'.tr() : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _countryCtrl,
                decoration: InputDecoration(
                  labelText: 'Country'.tr(),
                  hintText: 'e.g. Iraq, Turkey...'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                  prefixIcon: const Icon(Icons.flag_outlined),
                ),
              ),
              const SizedBox(height: 24),
              Text('Address Location'.tr(),
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
              const SizedBox(height: 12),
              SizedBox(
                height: 300,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(12),
                  child: Stack(
                    children: [
                      FlutterMap(
                        mapController: _mapController,
                        options: MapOptions(
                          initialCenter: _selectedLocation,
                          initialZoom: 14,
                          onTap: _onMapTap,
                        ),
                        children: [
                          TileLayer(
                            urlTemplate:
                                'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                            userAgentPackageName:
                                'com.example.online_store_mobile',
                          ),
                          MarkerLayer(
                            markers: [
                              Marker(
                                point: _selectedLocation,
                                width: 80,
                                height: 80,
                                child: const Icon(Icons.location_on,
                                    color: Colors.red, size: 40),
                              ),
                            ],
                          ),
                        ],
                      ),
                      Positioned(
                        right: 8,
                        top: 8,
                        child: FloatingActionButton.small(
                          onPressed: _getCurrentLocation,
                          child: const Icon(Icons.my_location),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),
              if (_accountType == 'Shop') ...[
                Text('Store Information'.tr(),
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _descriptionCtrl,
                  decoration: InputDecoration(
                    labelText: 'Description'.tr(),
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  maxLines: 3,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _openingTimeCtrl,
                        decoration: InputDecoration(
                          labelText: 'Opening Time (HH:mm)'.tr(),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextFormField(
                        controller: _closingTimeCtrl,
                        decoration: InputDecoration(
                          labelText: 'Closing Time (HH:mm)'.tr(),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
              ],
              Text('Address Details'.tr(),
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
              const SizedBox(height: 12),
              TextFormField(
                controller: _streetCtrl,
                decoration: InputDecoration(
                  labelText: 'Street'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _buildingCtrl,
                      decoration: InputDecoration(
                        labelText: 'Building No'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextFormField(
                      controller: _postalCtrl,
                      decoration: InputDecoration(
                        labelText: 'Postal Code'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _cityCtrl,
                decoration: InputDecoration(
                  labelText: 'City'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                ),
              ),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }
}
