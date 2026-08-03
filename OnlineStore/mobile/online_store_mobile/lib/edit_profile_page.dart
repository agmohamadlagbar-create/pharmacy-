import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:google_fonts/google_fonts.dart';
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
  final _cityCtrl = TextEditingController();
  final _districtCtrl = TextEditingController();
  final _neighborhoodCtrl = TextEditingController();
  final _streetCtrl = TextEditingController();
  final _buildingCtrl = TextEditingController();
  final _floorCtrl = TextEditingController();
  final _apartmentNoCtrl = TextEditingController();
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

  @override
  void dispose() {
    _nameCtrl.dispose();
    _countryCtrl.dispose();
    _cityCtrl.dispose();
    _districtCtrl.dispose();
    _neighborhoodCtrl.dispose();
    _streetCtrl.dispose();
    _buildingCtrl.dispose();
    _floorCtrl.dispose();
    _apartmentNoCtrl.dispose();
    _postalCtrl.dispose();
    _descriptionCtrl.dispose();
    _openingTimeCtrl.dispose();
    _closingTimeCtrl.dispose();
    super.dispose();
  }

  Future<void> _fetchProfile() async {
    try {
      final res = await Api.get('/api/me');
      setState(() {
        _nameCtrl.text = res['fullName']?.toString() ?? '';
        _countryCtrl.text = res['country']?.toString() ?? '';
        _cityCtrl.text = res['city']?.toString() ?? '';
        _districtCtrl.text = res['district']?.toString() ?? '';
        _neighborhoodCtrl.text = res['neighborhood']?.toString() ?? '';
        _streetCtrl.text = res['street']?.toString() ?? '';
        _buildingCtrl.text = res['buildingNumber']?.toString() ?? '';
        _floorCtrl.text = res['floor']?.toString() ?? '';
        _apartmentNoCtrl.text = res['apartmentNo']?.toString() ?? '';
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
            _districtCtrl.text = addr['city_district'] ??
                addr['suburb'] ??
                addr['county'] ??
                '';
            _neighborhoodCtrl.text = addr['neighbourhood'] ??
                addr['quarter'] ??
                addr['residential'] ??
                '';
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
    } catch (_) {}
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
        'city': _cityCtrl.text,
        'district': _districtCtrl.text,
        'neighborhood': _neighborhoodCtrl.text,
        'street': _streetCtrl.text,
        'buildingNumber': _buildingCtrl.text,
        'floor': _floorCtrl.text,
        'apartmentNo': _apartmentNoCtrl.text,
        'postalCode': _postalCtrl.text,
        'latitude': _selectedLocation.latitude,
        'longitude': _selectedLocation.longitude,
        if (_descriptionCtrl.text.isNotEmpty)
          'description': _descriptionCtrl.text,
        if (_openingTimeCtrl.text.isNotEmpty)
          'openingTime': _openingTimeCtrl.text,
        if (_closingTimeCtrl.text.isNotEmpty)
          'closingTime': _closingTimeCtrl.text,
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
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    if (_isLoading) {
      return Scaffold(
        backgroundColor: theme.scaffoldBackgroundColor,
        body: Center(child: CircularProgressIndicator(color: colorScheme.primary)),
      );
    }

    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      appBar: AppBar(
        backgroundColor: colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 1,
        title: Text(
          'Edit Profile'.tr(),
          style: GoogleFonts.inter(
              fontWeight: FontWeight.bold, color: colorScheme.onSurface),
        ),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_rounded, color: colorScheme.onSurface),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          if (_isSaving)
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(
                    strokeWidth: 2, color: colorScheme.primary),
              ),
            )
          else
            IconButton(
              icon: Icon(Icons.check_rounded, color: colorScheme.primary),
              onPressed: _save,
            ),
          const SizedBox(width: 8),
        ],
      ),
      body: Form(
        key: _formKey,
        child: SingleChildScrollView(
          physics: const BouncingScrollPhysics(),
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildSectionTitle(
                  context,
                  _accountType == 'Shop'
                      ? 'Store Information'.tr()
                      : 'Personal Information'.tr()),
              const SizedBox(height: 12),
              TextFormField(
                controller: _nameCtrl,
                style: GoogleFonts.inter(color: colorScheme.onSurface),
                decoration: InputDecoration(
                  labelText: _accountType == 'Shop'
                      ? 'Shop Name'.tr()
                      : 'Full Name *'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                  prefixIcon: const Icon(Icons.person_outline),
                ),
                validator: (v) =>
                    v!.isEmpty ? 'This field is required'.tr() : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _countryCtrl,
                style: GoogleFonts.inter(color: colorScheme.onSurface),
                decoration: InputDecoration(
                  labelText: 'Country'.tr(),
                  hintText: 'e.g. Iraq, Turkey...'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                  prefixIcon: const Icon(Icons.flag_outlined),
                ),
              ),
              const SizedBox(height: 24),

              _buildSectionTitle(context, 'Location Pin (Map)'.tr()),
              const SizedBox(height: 12),
              SizedBox(
                height: 240,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(16),
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
                                width: 50,
                                height: 50,
                                child: Icon(Icons.location_on_rounded,
                                    color: colorScheme.primary, size: 40),
                              ),
                            ],
                          ),
                        ],
                      ),
                      Positioned(
                        right: 12,
                        top: 12,
                        child: FloatingActionButton.small(
                          backgroundColor: colorScheme.surface,
                          foregroundColor: colorScheme.primary,
                          onPressed: _getCurrentLocation,
                          child: const Icon(Icons.my_location),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),

              if (_accountType == 'Shop') ...[
                _buildSectionTitle(context, 'Store Information'.tr()),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _descriptionCtrl,
                  style: GoogleFonts.inter(color: colorScheme.onSurface),
                  decoration: InputDecoration(
                    labelText: 'Description'.tr(),
                    border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12)),
                  ),
                  maxLines: 3,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _openingTimeCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'Opening Time (HH:mm)'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextFormField(
                        controller: _closingTimeCtrl,
                        style: GoogleFonts.inter(color: colorScheme.onSurface),
                        decoration: InputDecoration(
                          labelText: 'Closing Time (HH:mm)'.tr(),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12)),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
              ],

              _buildSectionTitle(
                  context,
                  _accountType == 'User'
                      ? 'Address Details (Required for Customer)'.tr()
                      : 'Address Details (Partially Required)'.tr()),
              const SizedBox(height: 12),

              // City and District Row
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _cityCtrl,
                      style: GoogleFonts.inter(color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        labelText: 'City *'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                      validator: (v) => (v == null || v.trim().isEmpty)
                          ? 'City is required'.tr()
                          : null,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextFormField(
                      controller: _districtCtrl,
                      style: GoogleFonts.inter(color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        labelText: 'District *'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                      validator: (v) => (v == null || v.trim().isEmpty)
                          ? 'District is required'.tr()
                          : null,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Neighborhood Field
              TextFormField(
                controller: _neighborhoodCtrl,
                style: GoogleFonts.inter(color: colorScheme.onSurface),
                decoration: InputDecoration(
                  labelText: 'Neighborhood / Quarter *'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                ),
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Neighborhood is required'.tr()
                    : null,
              ),
              const SizedBox(height: 12),

              // Street Field
              TextFormField(
                controller: _streetCtrl,
                style: GoogleFonts.inter(color: colorScheme.onSurface),
                decoration: InputDecoration(
                  labelText: 'Street / Address Line *'.tr(),
                  border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12)),
                ),
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Street is required'.tr()
                    : null,
              ),
              const SizedBox(height: 12),

              // Building No, Floor, Apartment No Row
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _buildingCtrl,
                      style: GoogleFonts.inter(color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        labelText: _accountType == 'User'
                            ? 'Bldg No *'.tr()
                            : 'Bldg No'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                      validator: (v) => (_accountType == 'User' && (v == null || v.trim().isEmpty))
                          ? 'Required'.tr()
                          : null,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextFormField(
                      controller: _floorCtrl,
                      style: GoogleFonts.inter(color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        labelText: _accountType == 'User'
                            ? 'Floor *'.tr()
                            : 'Floor'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                      validator: (v) => (_accountType == 'User' && (v == null || v.trim().isEmpty))
                          ? 'Required'.tr()
                          : null,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextFormField(
                      controller: _apartmentNoCtrl,
                      style: GoogleFonts.inter(color: colorScheme.onSurface),
                      decoration: InputDecoration(
                        labelText: _accountType == 'User'
                            ? 'Apt No *'.tr()
                            : 'Apt No'.tr(),
                        border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                      validator: (v) => (_accountType == 'User' && (v == null || v.trim().isEmpty))
                          ? 'Required'.tr()
                          : null,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              TextFormField(
                controller: _postalCtrl,
                style: GoogleFonts.inter(color: colorScheme.onSurface),
                decoration: InputDecoration(
                  labelText: 'Postal Code'.tr(),
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

  Widget _buildSectionTitle(BuildContext context, String title) {
    return Text(
      title,
      style: GoogleFonts.inter(
        fontSize: 12,
        fontWeight: FontWeight.bold,
        letterSpacing: 1.2,
        color: Theme.of(context).colorScheme.primary,
      ),
    );
  }
}
