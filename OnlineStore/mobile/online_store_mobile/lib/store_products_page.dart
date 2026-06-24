import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/skeleton_loader.dart';

class StoreProductsPage extends StatefulWidget {
  const StoreProductsPage({super.key});

  @override
  State<StoreProductsPage> createState() => _StoreProductsPageState();
}

class _StoreProductsPageState extends State<StoreProductsPage> {
  List<Map<String, dynamic>> _products = [];
  List<Map<String, dynamic>> _categories = [];
  List<Map<String, dynamic>> _subcategories = [];
  bool _loading = true;
  bool _showForm = false;
  String _storeType = '';
  int? _foodCategoryId;

  String _searchQuery = '';
  String _statusFilter = 'All';
  int? _editingId;

  // Form fields
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final _priceCtrl = TextEditingController();
  final _imageCtrl = TextEditingController();
  int? _selectedCategoryId;
  int? _selectedSubcategoryId;
  File? _selectedImageFile;
  bool _uploadingImage = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _descCtrl.dispose();
    _priceCtrl.dispose();
    _imageCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final meRes = await Api.get('/api/me');

      dynamic prodRes;
      try {
        prodRes = await Api.get('/api/products/mine');
      } catch (e) {
        // Handle 404 or empty properly by parsing the exception string
        if (e.toString().contains('404')) {
          prodRes = [];
        } else {
          rethrow;
        }
      }

      final catRes = await Api.get('/api/categories');

      if (mounted) {
        setState(() {
          _storeType = meRes['storeType'] ?? '';
          _products = _toList(prodRes);
          _categories = _toList(catRes);

          if (_storeType == 'Restaurant') {
            final foodCat = _categories.firstWhere(
                (c) => c['name']?.toString().toLowerCase() == 'food',
                orElse: () => <String, dynamic>{});
            if (foodCat.isNotEmpty) {
              _foodCategoryId = foodCat['id'] as int?;
              _selectedCategoryId = _foodCategoryId;
              if (_foodCategoryId != null) {
                _loadSubcategories(_foodCategoryId!);
              }
            }
          }

          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content:
                  Text('${'Error:'.tr()} ${e.toString().replaceAll("Exception: ", "")}'),
              backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  Future<void> _loadSubcategories(int parentId) async {
    try {
      final subRes = await Api.get('/api/categories/$parentId/subcategories');
      if (mounted) {
        setState(() {
          _subcategories = _toList(subRes);
          _selectedSubcategoryId = null; // Reset subcat selection
        });
      }
    } catch (e) {
      debugPrint('Failed to load subcategories: $e');
    }
  }

  List<Map<String, dynamic>> _toList(dynamic res) {
    if (res is List) {
      return res.map((e) => Map<String, dynamic>.from(e)).toList();
    }
    if (res is Map && res.containsKey('data')) {
      final d = res['data'];
      if (d is List) return d.map((e) => Map<String, dynamic>.from(e)).toList();
    }
    return [];
  }

  Future<void> _pickAndUploadImage() async {
    final picker = ImagePicker();
    final picked =
        await picker.pickImage(source: ImageSource.gallery, imageQuality: 80);
    if (picked == null) return;

    final file = File(picked.path);
    setState(() {
      _selectedImageFile = file;
      _uploadingImage = true;
    });

    try {
      final catId = _selectedSubcategoryId ?? _selectedCategoryId;
      final url = catId != null
          ? '/api/products/upload-image?categoryId=$catId'
          : '/api/products/upload-image';
      final res = await Api.upload(url, file);
      if (mounted) {
        setState(() {
          _imageCtrl.text = res['imageUrl'] ?? '';
          _uploadingImage = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _selectedImageFile = null;
          _uploadingImage = false;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content: Text(
                  '${'Image upload failed:'.tr()} ${e.toString().replaceAll("Exception: ", "")}'),
              backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  Future<void> _submitProduct() async {
    if (_nameCtrl.text.isEmpty ||
        _priceCtrl.text.isEmpty ||
        (_storeType != 'Restaurant' && _selectedCategoryId == null)) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
            content: Text('Please fill in name, price, and category'.tr()),
            backgroundColor: Colors.amber),
      );
      return;
    }

    // Use subcategory if selected, otherwise fallback to main category
    final finalCatId = _selectedSubcategoryId ?? _selectedCategoryId;
    if (finalCatId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
            content: Text('Please select a category'.tr()),
            backgroundColor: Colors.amber),
      );
      return;
    }

    try {
      final body = {
        'name': _nameCtrl.text,
        'description': _descCtrl.text,
        'price': double.tryParse(_priceCtrl.text) ?? 0,
        'imageUrl': _imageCtrl.text.isNotEmpty ? _imageCtrl.text : null,
        'categoryId': finalCatId,
      };

      if (_editingId != null) {
        await Api.put('/api/products/$_editingId', body);
      } else {
        await Api.post('/api/products', body);
      }

      _nameCtrl.clear();
      _descCtrl.clear();
      _priceCtrl.clear();
      _imageCtrl.clear();
      setState(() {
        _showForm = false;
        _editingId = null;
        _selectedImageFile = null;
        if (_storeType != 'Restaurant') {
          _selectedCategoryId = null;
          _subcategories = [];
        }
        _selectedSubcategoryId = null;
      });
      _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content: Text(_editingId != null
                  ? 'Product updated!'.tr()
                  : 'Product created! Waiting for admin approval.'.tr()),
              backgroundColor: Colors.green),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content: Text('${'Submit failed:'.tr()} $e'),
              backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  void _startEdit(Map<String, dynamic> p) {
    setState(() {
      _editingId = p['id'];
      _nameCtrl.text = p['name'] ?? '';
      _descCtrl.text = p['description'] ?? '';
      _priceCtrl.text = (p['price'] ?? '').toString();
      _imageCtrl.text = p['imageUrl'] ?? '';
      _selectedCategoryId = p['categoryId'] as int?;
      _selectedSubcategoryId = null; // reset subcategory properly next
      _showForm = true;
    });
    if (_selectedCategoryId != null) {
      _loadSubcategories(_selectedCategoryId!);
    }
  }

  Future<void> _deleteProduct(int id) async {
    try {
      await Api.delete('/api/products/$id');
      _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content: Text('${'Delete failed:'.tr()} $e'),
              backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Pending':
        return Colors.orange.shade800;
      case 'Approved':
        return Colors.green.shade700;
      case 'Rejected':
        return Colors.red.shade700;
      default:
        return Colors.grey.shade600;
    }
  }

  Widget _buildGlassCard(
      {required Widget child, EdgeInsets padding = const EdgeInsets.all(16)}) {
    final theme = Theme.of(context);
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: theme.dividerColor),
        boxShadow: [
          BoxShadow(
              color: Colors.black.withValues(alpha: 0.03),
              blurRadius: 10,
              offset: const Offset(0, 4))
        ],
      ),
      child: Padding(
        padding: padding,
        child: child,
      ),
    );
  }

  Widget _buildInput(String label, TextEditingController ctrl,
      {int maxLines = 1, TextInputType? type}) {
    final theme = Theme.of(context);
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: theme.dividerColor),
      ),
      child: TextField(
        controller: ctrl,
        maxLines: maxLines,
        keyboardType: type,
        style: TextStyle(color: theme.colorScheme.onSurface),
        decoration: InputDecoration(
          labelText: label,
          labelStyle: TextStyle(color: theme.colorScheme.onSurfaceVariant),
          border: InputBorder.none,
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final filteredProducts = _products.where((p) {
      final matchesSearch = (p['name']
                  ?.toString()
                  .toLowerCase()
                  .contains(_searchQuery.toLowerCase()) ??
              false) ||
          (p['description']
                  ?.toString()
                  .toLowerCase()
                  .contains(_searchQuery.toLowerCase()) ??
              false);
      final status = p['status']?.toString() ?? 'Pending';
      final matchesStatus = _statusFilter == 'All' || status == _statusFilter;
      return matchesSearch && matchesStatus;
    }).toList();

    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: Padding(
          padding: const EdgeInsets.all(8.0),
          child: InkWell(
            onTap: () => Navigator.pop(context),
            borderRadius: BorderRadius.circular(24),
            child: Container(
              decoration: BoxDecoration(
                color: const Color(0xFFD85A30).withValues(alpha: 0.1),
                border: Border.all(color: const Color(0xFFD85A30).withValues(alpha: 0.5)),
                borderRadius: BorderRadius.circular(24),
              ),
              child: const Icon(Icons.arrow_back, color: Color(0xFFD85A30)),
            ),
          ),
        ),
        title: Text(
            _showForm
                ? (_editingId != null ? 'Edit Product'.tr() : 'Create Product'.tr())
                : 'My Products'.tr(),
            style: TextStyle(
                fontWeight: FontWeight.bold, color: Theme.of(context).colorScheme.onSurface)),
        centerTitle: true,
        actions: [
          if (!_showForm)
            IconButton(
              icon: Icon(Icons.add_circle_outline, color: Theme.of(context).colorScheme.onSurface),
              onPressed: () => setState(() {
                _showForm = true;
                _editingId = null;
                _nameCtrl.clear();
                _descCtrl.clear();
                _priceCtrl.clear();
                _imageCtrl.clear();
                if (_storeType != 'Restaurant') _selectedCategoryId = null;
                _selectedSubcategoryId = null;
              }),
            ),
          if (_showForm)
            IconButton(
              icon: Icon(Icons.close, color: Theme.of(context).colorScheme.onSurface),
              onPressed: () => setState(() {
                _showForm = false;
                _editingId = null;
              }),
            ),
        ],
      ),
      body: SafeArea(
          child: Column(
            children: [
              if (!_showForm) ...[
                // Search Bar
                Padding(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: Container(
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                      borderRadius: BorderRadius.circular(24),
                      border: Border.all(color: Theme.of(context).dividerColor),
                    ),
                    child: TextField(
                      style: TextStyle(color: Theme.of(context).colorScheme.onSurface),
                      onChanged: (v) => setState(() => _searchQuery = v),
                      decoration: InputDecoration(
                        hintText: 'Search products...'.tr(),
                        hintStyle:
                            TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant.withValues(alpha: 0.5)),
                        prefixIcon: Icon(Icons.search,
                            color: Theme.of(context).dividerColor),
                        border: InputBorder.none,
                        contentPadding:
                            const EdgeInsets.symmetric(vertical: 14),
                      ),
                    ),
                  ),
                ),
                // Filter Pills
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: Row(
                    children: ['All', 'Approved', 'Pending'].map((status) {
                      final isActive = _statusFilter == status;
                      final theme = Theme.of(context);
                      return Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: InkWell(
                          onTap: () => setState(() => _statusFilter = status),
                          borderRadius: BorderRadius.circular(20),
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 16, vertical: 8),
                            decoration: BoxDecoration(
                              color: isActive
                                  ? theme.colorScheme.primary.withValues(alpha: 0.1)
                                  : Colors.transparent,
                              borderRadius: BorderRadius.circular(20),
                              border: Border.all(
                                  color: isActive
                                      ? theme.colorScheme.primary
                                      : theme.dividerColor),
                            ),
                            child: Text(status.tr(),
                                style: TextStyle(
                                    color: isActive
                                        ? theme.colorScheme.primary
                                        : theme.colorScheme.onSurfaceVariant,
                                    fontWeight: isActive
                                        ? FontWeight.bold
                                        : FontWeight.normal)),
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ],
              Expanded(
                child: _loading
                    ? Shimmer(
                        child: ListView.builder(
                          padding: const EdgeInsets.only(bottom: 24),
                          itemCount: 5,
                          itemBuilder: (context, i) => _buildGlassCard(
                            padding: EdgeInsets.zero,
                            child: Padding(
                              padding: const EdgeInsets.all(16),
                              child: Row(
                                children: [
                                  const Skeleton(
                                      width: 70,
                                      height: 70,
                                      borderRadius: BorderRadius.all(
                                          Radius.circular(12))),
                                  const SizedBox(width: 16),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        const Skeleton(width: 120, height: 16),
                                        const SizedBox(height: 8),
                                        const Skeleton(width: 180, height: 12),
                                        const SizedBox(height: 12),
                                        Row(
                                          children: [
                                            Skeleton(
                                                width: 60,
                                                height: 24,
                                                borderRadius:
                                                    BorderRadius.circular(12)),
                                            const SizedBox(width: 8),
                                            Skeleton(
                                                width: 60,
                                                height: 24,
                                                borderRadius:
                                                    BorderRadius.circular(12)),
                                          ],
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      )
                    : _showForm
                        ? SingleChildScrollView(
                            child: _buildGlassCard(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  _buildInput('Name'.tr(), _nameCtrl),
                                  _buildInput('Description'.tr(), _descCtrl,
                                      maxLines: 2),
                                  _buildInput('Price'.tr(), _priceCtrl,
                                      type: TextInputType.number),
                                  // Image picker area
                                  GestureDetector(
                                    onTap: _uploadingImage
                                        ? null
                                        : _pickAndUploadImage,
                                    child: Container(
                                      margin: const EdgeInsets.only(bottom: 12),
                                      height: 150,
                                      decoration: BoxDecoration(
                                        color: Theme.of(context).colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                                        borderRadius: BorderRadius.circular(12),
                                        border: Border.all(
                                            color:
                                                Theme.of(context).dividerColor),
                                      ),
                                      child: _uploadingImage
                                          ? const Center(
                                              child:
                                                  CircularProgressIndicator())
                                          : (_selectedImageFile != null)
                                              ? Stack(
                                                  fit: StackFit.expand,
                                                  children: [
                                                    ClipRRect(
                                                      borderRadius:
                                                          BorderRadius.circular(
                                                              12),
                                                      child: Image.file(
                                                          _selectedImageFile!,
                                                          fit: BoxFit.cover),
                                                    ),
                                                    Positioned(
                                                      top: 6,
                                                      right: 6,
                                                      child: GestureDetector(
                                                        onTap: () =>
                                                            setState(() {
                                                          _selectedImageFile =
                                                              null;
                                                          _imageCtrl.clear();
                                                        }),
                                                        child: Container(
                                                          padding:
                                                              const EdgeInsets
                                                                  .all(4),
                                                          decoration:
                                                              BoxDecoration(
                                                            color:
                                                                Colors.black54,
                                                            borderRadius:
                                                                BorderRadius
                                                                    .circular(
                                                                        16),
                                                          ),
                                                          child: const Icon(
                                                              Icons.close,
                                                              color:
                                                                  Colors.white,
                                                              size: 18),
                                                        ),
                                                      ),
                                                    ),
                                                  ],
                                                )
                                              : (_imageCtrl.text.isNotEmpty)
                                                  ? Stack(
                                                      fit: StackFit.expand,
                                                      children: [
                                                        ClipRRect(
                                                          borderRadius:
                                                              BorderRadius
                                                                  .circular(12),
                                                          child: Image.network(
                                                            _imageCtrl.text
                                                                    .startsWith(
                                                                        'http')
                                                                ? _imageCtrl
                                                                    .text
                                                                : '${Api.baseUrl}${_imageCtrl.text}',
                                                            fit: BoxFit.cover,
                                                            errorBuilder: (_,
                                                                    __, ___) =>
                                                                const Center(
                                                              child: Icon(
                                                                  Icons
                                                                      .broken_image,
                                                                  color: Colors
                                                                      .grey,
                                                                  size: 40),
                                                            ),
                                                          ),
                                                        ),
                                                        Positioned(
                                                          top: 6,
                                                          right: 6,
                                                          child:
                                                              GestureDetector(
                                                            onTap: () =>
                                                                setState(() {
                                                              _imageCtrl
                                                                  .clear();
                                                            }),
                                                            child: Container(
                                                              padding:
                                                                  const EdgeInsets
                                                                      .all(4),
                                                              decoration:
                                                                  BoxDecoration(
                                                                color: Colors
                                                                    .black54,
                                                                borderRadius:
                                                                    BorderRadius
                                                                        .circular(
                                                                            16),
                                                              ),
                                                              child: const Icon(
                                                                  Icons.close,
                                                                  color: Colors
                                                                      .white,
                                                                  size: 18),
                                                            ),
                                                          ),
                                                        ),
                                                      ],
                                                    )
                                                  : Column(
                                                      mainAxisAlignment:
                                                          MainAxisAlignment
                                                              .center,
                                                      children: [
                                                        Icon(
                                                            Icons
                                                                .add_photo_alternate_outlined,
                                                            color: Theme.of(context).colorScheme.onSurfaceVariant.withValues(alpha: 
                                                                    0.3),
                                                            size: 40),
                                                        const SizedBox(
                                                            height: 8),
                                                        Text(
                                                            'Tap to upload product image'.tr(),
                                                            style: TextStyle(
                                                                color: Theme.of(context).colorScheme.onSurfaceVariant.withValues(alpha: 
                                                                    0.3),
                                                                fontSize: 13)),
                                                      ],
                                                    ),
                                    ),
                                  ),
                                  const SizedBox(height: 8),
                                  if (_storeType == 'Restaurant')
                                    Padding(
                                      padding:
                                          const EdgeInsets.only(bottom: 12),
                                      child: Text(
                                          'Category: Food (Restaurant mapping)'.tr(),
                                          style: const TextStyle(
                                              color: Color(0xFFD85A30),
                                              fontWeight: FontWeight.bold)),
                                    )
                                  else
                                    Container(
                                      margin: const EdgeInsets.only(bottom: 12),
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 16),
                                      decoration: BoxDecoration(
                                        color: Theme.of(context).colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                                        borderRadius: BorderRadius.circular(12),
                                      ),
                                      child: DropdownButtonHideUnderline(
                                        child: DropdownButton<int>(
                                          value: _selectedCategoryId != null &&
                                                  _categories.any((c) =>
                                                      c['id'] ==
                                                      _selectedCategoryId)
                                              ? _selectedCategoryId
                                              : null,
                                          hint: Text('Main Category'.tr(),
                                              style: TextStyle(
                                                  color: Theme.of(context).colorScheme.onSurfaceVariant.withValues(alpha: 0.5))),
                                          dropdownColor: Theme.of(context)
                                              .colorScheme.surface,
                                          style: TextStyle(
                                              color: Theme.of(context).colorScheme.onSurface),
                                          isExpanded: true,
                                          items: _categories.map((c) {
                                            return DropdownMenuItem<int>(
                                                value:
                                                    (c['id'] as num?)?.toInt(),
                                                child: Text(
                                                    c['name']?.toString() ??
                                                        ''));
                                          }).toList(),
                                          onChanged: (v) {
                                            setState(
                                                () => _selectedCategoryId = v);
                                            if (v != null) {
                                              _loadSubcategories(v);
                                            }
                                          },
                                        ),
                                      ),
                                    ),
                                  if (_subcategories.isNotEmpty ||
                                      (_storeType == 'Restaurant' &&
                                          _foodCategoryId != null))
                                    Container(
                                      margin: const EdgeInsets.only(bottom: 16),
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 16),
                                      decoration: BoxDecoration(
                                        color: Theme.of(context).colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                                        borderRadius: BorderRadius.circular(12),
                                      ),
                                      child: DropdownButtonHideUnderline(
                                        child: DropdownButton<int>(
                                          value: _selectedSubcategoryId !=
                                                      null &&
                                                  _subcategories.any((c) =>
                                                      c['id'] ==
                                                      _selectedSubcategoryId)
                                              ? _selectedSubcategoryId
                                              : null,
                                          hint: Text('Subcategory (Optional)'.tr(),
                                              style: TextStyle(
                                                  color: Theme.of(context).colorScheme.onSurfaceVariant.withValues(alpha: 0.5))),
                                          dropdownColor: Theme.of(context)
                                              .colorScheme.surface,
                                          style: TextStyle(
                                              color: Theme.of(context).colorScheme.onSurface),
                                          isExpanded: true,
                                          items: _subcategories.map((c) {
                                            return DropdownMenuItem<int>(
                                                value:
                                                    (c['id'] as num?)?.toInt(),
                                                child: Text(
                                                    c['name']?.toString() ??
                                                        ''));
                                          }).toList(),
                                          onChanged: (v) => setState(
                                              () => _selectedSubcategoryId = v),
                                        ),
                                      ),
                                    ),
                                  const SizedBox(height: 14),
                                  InkWell(
                                    onTap: _submitProduct,
                                    borderRadius: BorderRadius.circular(12),
                                    child: Container(
                                      width: double.infinity,
                                      padding: const EdgeInsets.symmetric(
                                          vertical: 16),
                                      decoration: BoxDecoration(
                                        gradient: const LinearGradient(colors: [
                                          Color(0xFFD85A30),
                                          Color(0xFF993C1D)
                                        ]),
                                        borderRadius: BorderRadius.circular(12),
                                        boxShadow: [
                                          BoxShadow(
                                              color: const Color(0xFFD85A30)
                                                  .withValues(alpha: 0.4),
                                              blurRadius: 12,
                                              offset: const Offset(0, 4))
                                        ],
                                      ),
                                      child: Center(
                                        child: Text(
                                          _editingId != null
                                              ? 'Update Product'.tr()
                                              : 'Create Product'.tr(),
                                          style: const TextStyle(
                                              color: Colors.white,
                                              fontWeight: FontWeight.bold,
                                              fontSize: 16),
                                        ),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          )
                        : filteredProducts.isEmpty
                            ? Center(
                                child: Text('No products matched.'.tr(),
                                    style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant)),
                              )
                            : ListView.builder(
                                padding: const EdgeInsets.only(bottom: 24),
                                itemCount: filteredProducts.length,
                                itemBuilder: (context, i) {
                                  final p = filteredProducts[i];
                                  final status =
                                      p['status']?.toString() ?? 'Pending';
                                  final price =
                                      (p['price'] as num?)?.toDouble() ?? 0;
                                  final imageUrl = p['imageUrl']?.toString();
                                  final theme = Theme.of(context);

                                  return _buildGlassCard(
                                    padding: EdgeInsets.zero,
                                    child: Padding(
                                      padding: const EdgeInsets.all(16),
                                      child: Row(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          // Thumbnail
                                          Container(
                                            width: 70,
                                            height: 70,
                                            decoration: BoxDecoration(
                                              color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                                              borderRadius:
                                                  BorderRadius.circular(12),
                                              border: Border.all(color: theme.dividerColor),
                                              image: (imageUrl != null &&
                                                      imageUrl.isNotEmpty)
                                                  ? DecorationImage(
                                                      image: NetworkImage(imageUrl
                                                              .startsWith(
                                                                  'http')
                                                          ? imageUrl
                                                          : '${Api.baseUrl}$imageUrl'),
                                                      fit: BoxFit.cover,
                                                      onError: (_, __) =>
                                                          const SizedBox(),
                                                    )
                                                  : null,
                                            ),
                                            child: (imageUrl == null ||
                                                    imageUrl.isEmpty)
                                                ? Icon(Icons.image,
                                                    color: theme.dividerColor,
                                                    size: 30)
                                                : null,
                                          ),
                                          const SizedBox(width: 16),
                                          // Content
                                          Expanded(
                                            child: Column(
                                              crossAxisAlignment:
                                                  CrossAxisAlignment.start,
                                              children: [
                                                Row(
                                                  mainAxisAlignment:
                                                      MainAxisAlignment
                                                          .spaceBetween,
                                                  children: [
                                                    Expanded(
                                                      child: Text(
                                                        p['name'] ?? '',
                                                        style: TextStyle(
                                                            color: theme.colorScheme.onSurface,
                                                            fontWeight:
                                                                FontWeight.bold,
                                                            fontSize: 16),
                                                        maxLines: 1,
                                                        overflow: TextOverflow
                                                            .ellipsis,
                                                      ),
                                                    ),
                                                    Container(
                                                      padding: const EdgeInsets
                                                          .symmetric(
                                                          horizontal: 8,
                                                          vertical: 4),
                                                      decoration: BoxDecoration(
                                                        color: _statusColor(
                                                                status)
                                                            .withValues(alpha: 0.15),
                                                        borderRadius:
                                                            BorderRadius
                                                                .circular(12),
                                                        border: Border.all(
                                                            color: _statusColor(
                                                                    status)
                                                                .withValues(alpha: 
                                                                    0.3)),
                                                      ),
                                                      child: Text(status.tr(),
                                                          style: TextStyle(
                                                              color:
                                                                  _statusColor(
                                                                      status),
                                                              fontSize: 10,
                                                              fontWeight:
                                                                  FontWeight
                                                                      .bold)),
                                                    ),
                                                  ],
                                                ),
                                                const SizedBox(height: 6),
                                                Text(
                                                    '\$${price.toStringAsFixed(2)} • ${p['categoryName'] ?? ''}',
                                                    style: TextStyle(
                                                        color: theme.colorScheme.onSurfaceVariant,
                                                        fontSize: 13)),
                                                if (p['rejectionReason'] !=
                                                        null &&
                                                    p['rejectionReason']
                                                        .toString()
                                                        .isNotEmpty)
                                                  Padding(
                                                    padding:
                                                        const EdgeInsets.only(
                                                            top: 8),
                                                    child: Text(
                                                        '${'Reason:'.tr()} ${p['rejectionReason']}',
                                                        style: const TextStyle(
                                                            color: Colors
                                                                .redAccent,
                                                            fontSize: 12)),
                                                  ),
                                                const SizedBox(height: 12),
                                                Row(
                                                  children: [
                                                    InkWell(
                                                      onTap: () =>
                                                          _startEdit(p),
                                                      borderRadius:
                                                          BorderRadius.circular(
                                                              8),
                                                      child: Container(
                                                        padding:
                                                            const EdgeInsets
                                                                .symmetric(
                                                                horizontal: 16,
                                                                vertical: 8),
                                                        decoration:
                                                            BoxDecoration(
                                                          color: theme.colorScheme.primary.withValues(alpha: 0.1),
                                                          border: Border.all(
                                                              color: theme.colorScheme.primary),
                                                          borderRadius:
                                                              BorderRadius
                                                                  .circular(8),
                                                        ),
                                                        child: Text(
                                                            'Edit'.tr(),
                                                            style: TextStyle(
                                                                color: theme.colorScheme.primary,
                                                                fontSize: 12,
                                                                fontWeight:
                                                                    FontWeight
                                                                        .bold)),
                                                      ),
                                                    ),
                                                    const SizedBox(width: 12),
                                                    InkWell(
                                                      onTap: () =>
                                                          _deleteProduct(
                                                              p['id'] as int),
                                                      borderRadius:
                                                          BorderRadius.circular(
                                                              8),
                                                      child: Container(
                                                        padding:
                                                            const EdgeInsets
                                                                .symmetric(
                                                                horizontal: 16,
                                                                vertical: 8),
                                                        decoration:
                                                            BoxDecoration(
                                                          color: Colors.red.shade50,
                                                          border: Border.all(
                                                              color: Colors.red.shade200),
                                                          borderRadius:
                                                              BorderRadius
                                                                  .circular(8),
                                                        ),
                                                        child: Text(
                                                            'Delete'.tr(),
                                                            style: TextStyle(
                                                                color: Colors.red.shade700,
                                                                fontSize: 12,
                                                                fontWeight:
                                                                    FontWeight
                                                                        .bold)),
                                                      ),
                                                    ),
                                                  ],
                                                )
                                              ],
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  );
                                },
                              ),
              ),
            ],
          ),
        ),
    );
  }
}

