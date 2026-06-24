import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:easy_localization/easy_localization.dart';
import 'api.dart';
import 'widgets/skeleton_loader.dart';

class AdminPage extends StatefulWidget {
  const AdminPage({super.key});

  @override
  State<AdminPage> createState() => _AdminPageState();
}

class _AdminPageState extends State<AdminPage>
    with SingleTickerProviderStateMixin {
  late TabController _tabCtrl;
  List<Map<String, dynamic>> _products = [];
  List<Map<String, dynamic>> _users = [];
  List<Map<String, dynamic>> _orders = [];
  List<Map<String, dynamic>> _categories = [];
  Map<int, List<Map<String, dynamic>>> _subcategories = {};
  List<Map<String, dynamic>> _ads = [];
  bool _loading = true;
  String _statusFilter = 'Pending';

  // Category form
  final _catNameCtrl = TextEditingController();
  final _catCodeCtrl = TextEditingController();
  int? _catParentId;
  File? _catImageFile;
  bool _creating = false;

  // Ad form
  File? _adImageFile;
  final _adTitleCtrl = TextEditingController();
  final _adLinkUrlCtrl = TextEditingController();
  final _adSortOrderCtrl = TextEditingController(text: '0');
  bool _adCreating = false;

  // Product form
  bool _showProductForm = false;
  int? _editingProductId;
  final _prodNameCtrl = TextEditingController();
  final _prodDescCtrl = TextEditingController();
  final _prodPriceCtrl = TextEditingController();
  final _prodStoreUserIdCtrl = TextEditingController();
  int? _prodCategoryId;
  int? _prodSubcategoryId;
  String? _prodImageUrl;
  File? _prodImageFile;
  bool _prodSubmitting = false;

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 5, vsync: this);
    _tabCtrl.addListener(() {
      if (!_tabCtrl.indexIsChanging) _load();
    });
    _load();
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    _catNameCtrl.dispose();
    _catCodeCtrl.dispose();
    _adTitleCtrl.dispose();
    _adLinkUrlCtrl.dispose();
    _adSortOrderCtrl.dispose();
    _prodNameCtrl.dispose();
    _prodDescCtrl.dispose();
    _prodPriceCtrl.dispose();
    _prodStoreUserIdCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final tabIndex = _tabCtrl.index;
      final ts = DateTime.now().millisecondsSinceEpoch;
      if (tabIndex == 0) {
        final res =
            await Api.get('/api/admin/products?status=$_statusFilter&_=$ts');
        if (mounted) setState(() => _products = _toList(res));
        if (_categories.isEmpty) await _fetchCategoriesAndSubs();
      } else if (tabIndex == 1) {
        final res = await Api.get('/api/admin/users?_=$ts');
        if (mounted) setState(() => _users = _toList(res));
      } else if (tabIndex == 2) {
        final res = await Api.get('/api/admin/orders?_=$ts');
        if (mounted) setState(() => _orders = _toList(res));
      } else if (tabIndex == 3) {
        await _fetchCategoriesAndSubs();
      } else if (tabIndex == 4) {
        final res = await Api.get('/api/ads?_=$ts');
        if (mounted) setState(() => _ads = _toList(res));
      }
    } catch (e) {
      if (mounted) _showError('Error: $e');
    }
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _fetchCategoriesAndSubs() async {
    try {
      final ts = DateTime.now().millisecondsSinceEpoch;
      final res = await Api.get('/api/categories?_=$ts');
      final cats = _toList(res);
      final subs = <int, List<Map<String, dynamic>>>{};
      for (var c in cats) {
        try {
          final sRes =
              await Api.get('/api/categories/${c['id']}/subcategories?_=$ts');
          subs[c['id']] = _toList(sRes);
        } catch (_) {}
      }
      if (mounted) {
        setState(() {
          _categories = cats;
          _subcategories = subs;
        });
      }
    } catch (_) {}
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

  // ── Product actions ──
  Future<void> _approveProduct(int id) async {
    try {
      await Api.put('/api/admin/products/$id/approve', {});
      _load();
    } catch (e) {
      if (mounted) _showError('Approve failed: $e');
    }
  }

  Future<void> _rejectProduct(int id) async {
    final reason = await _showInputDialog(
        'Rejection Reason'.tr(), 'Enter the reason for rejection'.tr());
    if (reason == null || reason.isEmpty) return;
    try {
      await Api.put('/api/admin/products/$id/reject', {'reason': reason});
      _load();
    } catch (e) {
      if (mounted) _showError('Reject failed: $e');
    }
  }

  Future<void> _deleteProduct(int id) async {
    final confirm =
        await _showConfirmDialog('Delete this product permanently?'.tr());
    if (confirm != true) return;
    try {
      await Api.delete('/api/admin/products/$id');
      _load();
    } catch (e) {
      if (mounted) _showError('Delete failed: $e');
    }
  }

  void _startEditProduct(Map<String, dynamic> p) {
    setState(() {
      _showProductForm = true;
      _editingProductId = p['id'] as int;
      _prodNameCtrl.text = p['name'] ?? '';
      _prodDescCtrl.text = p['description'] ?? '';
      _prodPriceCtrl.text = (p['price'] as num?)?.toString() ?? '';
      _prodImageUrl = p['imageUrl']?.toString();
      _prodStoreUserIdCtrl.text = p['storeUserId'] ?? '';
      _prodImageFile = null;

      int? catId = (p['categoryId'] as num?)?.toInt();
      int? subCatId;
      if (catId != null) {
        for (final entry in _subcategories.entries) {
          if (entry.value.any((sub) => sub['id'] == catId)) {
            subCatId = catId;
            catId = entry.key;
            break;
          }
        }
      }
      _prodCategoryId = catId;
      _prodSubcategoryId = subCatId;
    });
  }

  Future<void> _pickProductImage() async {
    final picked = await ImagePicker()
        .pickImage(source: ImageSource.gallery, imageQuality: 80);
    if (picked == null) return;
    final file = File(picked.path);
    setState(() => _prodImageFile = file);
    try {
      final res = await Api.upload('/api/admin/products/upload-image', file);
      if (mounted) setState(() => _prodImageUrl = res['imageUrl']);
    } catch (e) {
      if (mounted) _showError('Image upload failed: $e');
    }
  }

  Future<void> _submitProduct() async {
    if (_prodNameCtrl.text.trim().isEmpty ||
        _prodPriceCtrl.text.trim().isEmpty ||
        _prodCategoryId == null) {
      return;
    }
    setState(() => _prodSubmitting = true);
    try {
      final finalCategoryId = _prodSubcategoryId ?? _prodCategoryId;
      if (_editingProductId != null) {
        await Api.put('/api/admin/products/$_editingProductId', {
          'name': _prodNameCtrl.text.trim(),
          'description': _prodDescCtrl.text.trim().isEmpty
              ? null
              : _prodDescCtrl.text.trim(),
          'price': double.parse(_prodPriceCtrl.text.trim()),
          'imageUrl': _prodImageUrl,
          'categoryId': finalCategoryId,
        });
      } else {
        await Api.post('/api/admin/products', {
          'name': _prodNameCtrl.text.trim(),
          'description': _prodDescCtrl.text.trim().isEmpty
              ? null
              : _prodDescCtrl.text.trim(),
          'price': double.parse(_prodPriceCtrl.text.trim()),
          'imageUrl': _prodImageUrl,
          'categoryId': finalCategoryId,
          'storeUserId': _prodStoreUserIdCtrl.text.trim(),
        });
      }
      setState(() {
        _showProductForm = false;
        _editingProductId = null;
        _prodNameCtrl.clear();
        _prodDescCtrl.clear();
        _prodPriceCtrl.clear();
        _prodStoreUserIdCtrl.clear();
        _prodCategoryId = null;
        _prodSubcategoryId = null;
        _prodImageUrl = null;
        _prodImageFile = null;
      });
      _load();
    } catch (e) {
      if (mounted) _showError('Failed: $e');
    } finally {
      if (mounted) setState(() => _prodSubmitting = false);
    }
  }

  // ── User actions ──
  Future<void> _toggleUser(String userId, bool isDisabled) async {
    try {
      if (isDisabled) {
        await Api.put('/api/admin/users/$userId/enable', {});
      } else {
        await Api.delete('/api/admin/users/$userId');
      }
      _load();
    } catch (e) {
      if (mounted) _showError('Action failed: $e');
    }
  }

  // ── Category actions ──
  Future<void> _createCategory() async {
    if (_catNameCtrl.text.trim().isEmpty) return;
    setState(() => _creating = true);
    try {
      String? imageUrl;
      if (_catImageFile != null) {
        final uploadRes =
            await Api.upload('/api/categories/upload-image', _catImageFile!);
        imageUrl = uploadRes['imageUrl'];
      }
      await Api.post('/api/categories', {
        'name': _catNameCtrl.text.trim(),
        'code':
            _catCodeCtrl.text.trim().isEmpty ? null : _catCodeCtrl.text.trim(),
        'imageUrl': imageUrl,
        'parentId': _catParentId,
      });
      _catNameCtrl.clear();
      _catCodeCtrl.clear();
      setState(() {
        _catParentId = null;
        _catImageFile = null;
      });
      _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text('Category created'.tr()), backgroundColor: Colors.green));
      }
    } catch (e) {
      if (mounted) _showError('Failed to create category: $e');
    } finally {
      if (mounted) setState(() => _creating = false);
    }
  }

  Future<void> _deleteCategory(int id) async {
    final confirm = await _showConfirmDialog('Delete this category?'.tr());
    if (confirm != true) return;
    try {
      await Api.delete('/api/categories/$id');
      _load();
    } catch (e) {
      if (mounted) _showError('Failed to delete: $e');
    }
  }

  Future<void> _changeCategoryImage(int catId) async {
    final picked = await ImagePicker()
        .pickImage(source: ImageSource.gallery, imageQuality: 80);
    if (picked == null) return;
    try {
      final uploadRes =
          await Api.upload('/api/categories/upload-image', File(picked.path));
      final cat =
          _categories.firstWhere((c) => c['id'] == catId, orElse: () => {});
      await Api.put('/api/categories/$catId', {
        'name': cat['name'] ?? '',
        'imageUrl': uploadRes['imageUrl'],
      });
      _load();
    } catch (e) {
      if (mounted) _showError('Failed: $e');
    }
  }

  Future<void> _removeCategoryImage(int catId) async {
    final cat =
        _categories.firstWhere((c) => c['id'] == catId, orElse: () => {});
    try {
      await Api.put('/api/categories/$catId', {
        'name': cat['name'] ?? '',
        'imageUrl': null,
      });
      _load();
    } catch (e) {
      if (mounted) _showError('Failed: $e');
    }
  }

  // ── Ad Banner actions ──
  Future<void> _createAd() async {
    if (_adImageFile == null) {
      _showError('Please select an image'.tr());
      return;
    }
    setState(() => _adCreating = true);
    try {
      final uploadRes =
          await Api.upload('/api/ads/upload-image', _adImageFile!);
      await Api.post('/api/ads', {
        'imageUrl': uploadRes['imageUrl'],
        'title':
            _adTitleCtrl.text.trim().isEmpty ? null : _adTitleCtrl.text.trim(),
        'linkUrl': _adLinkUrlCtrl.text.trim().isEmpty
            ? null
            : _adLinkUrlCtrl.text.trim(),
        'sortOrder': int.tryParse(_adSortOrderCtrl.text) ?? 0,
      });
      _adTitleCtrl.clear();
      _adLinkUrlCtrl.clear();
      _adSortOrderCtrl.text = '0';
      setState(() => _adImageFile = null);
      await _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text('Ad banner created'.tr()), backgroundColor: Colors.green));
      }
    } catch (e) {
      if (mounted) _showError('Failed to create ad: $e');
    } finally {
      if (mounted) setState(() => _adCreating = false);
    }
  }

  Future<void> _deleteAd(int id) async {
    final confirm = await _showConfirmDialog('Delete this ad banner?'.tr());
    if (confirm != true) return;
    try {
      await Api.delete('/api/ads/$id');
      await _load();
    } catch (e) {
      if (mounted) _showError('Failed to delete ad: $e');
    }
  }

  // ── Helpers ──
  void _showError(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(msg), backgroundColor: Colors.redAccent),
    );
  }

  Future<String?> _showInputDialog(String title, String hint) async {
    String value = '';
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(title),
        content: TextField(
          autofocus: true,
          decoration: InputDecoration(hintText: hint),
          onChanged: (v) => value = v,
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx), child: Text('Cancel'.tr())),
          FilledButton(
              onPressed: () => Navigator.pop(ctx, value),
              child: Text('Submit'.tr())),
        ],
      ),
    );
  }

  Future<bool?> _showConfirmDialog(String message) {
    return showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Confirm'.tr()),
        content: Text(message),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: Text('Cancel'.tr())),
          FilledButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: Text('Confirm'.tr())),
        ],
      ),
    );
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

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text('Admin Panel'.tr()),
        bottom: TabBar(
          controller: _tabCtrl,
          isScrollable: true,
          tabs: [
            Tab(icon: const Icon(Icons.inventory_2_rounded), text: 'Products'.tr()),
            Tab(icon: const Icon(Icons.people_rounded), text: 'Users'.tr()),
            Tab(icon: const Icon(Icons.receipt_long_rounded), text: 'Orders'.tr()),
            Tab(icon: const Icon(Icons.category_rounded), text: 'Categories'.tr()),
            Tab(icon: const Icon(Icons.photo_library_rounded), text: 'Ads'.tr()),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabCtrl,
        children: [
          _buildProductsTab(theme),
          _buildUsersTab(theme),
          _buildOrdersTab(theme),
          _buildCategoriesTab(theme),
          _buildAdsTab(theme),
        ],
      ),
    );
  }

  // ══════════════ Products Tab ══════════════
  Widget _buildProductsTab(ThemeData theme) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            children: [
              if (!_showProductForm)
                ...['Pending', 'Approved', 'Rejected'].map((s) {
                  final selected = _statusFilter == s;
                  return Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: FilterChip(
                      label: Text(s.tr()),
                      selected: selected,
                      selectedColor: _statusColor(s).withValues(alpha: 0.2),
                      checkmarkColor: _statusColor(s),
                      labelStyle: TextStyle(
                        color: selected
                            ? _statusColor(s)
                            : theme.colorScheme.onSurface,
                        fontWeight:
                            selected ? FontWeight.bold : FontWeight.normal,
                      ),
                      onSelected: (_) {
                        setState(() => _statusFilter = s);
                        _load();
                      },
                    ),
                  );
                }),
              const Spacer(),
              IconButton(
                icon: Icon(_showProductForm ? Icons.close : Icons.add_rounded),
                onPressed: () => setState(() {
                  _showProductForm = !_showProductForm;
                  if (!_showProductForm) _editingProductId = null;
                }),
              ),
            ],
          ),
        ),
        if (_showProductForm)
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Container(
                margin: const EdgeInsets.only(bottom: 16),
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface,
                  borderRadius: BorderRadius.circular(24),
                  border: Border.all(color: theme.dividerColor),
                  boxShadow: [
                    BoxShadow(
                        color: Colors.black.withValues(alpha: 0.05),
                        blurRadius: 20,
                        offset: const Offset(0, 10))
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                        _editingProductId != null
                            ? 'Edit Product'.tr()
                            : 'Create Product (Admin)'.tr(),
                        style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 18,
                            color: theme.colorScheme.onSurface)),
                    const SizedBox(height: 16),
                    _buildAdminInput('Name'.tr(), _prodNameCtrl),
                    _buildAdminInput('Description'.tr(), _prodDescCtrl, maxLines: 2),
                    _buildAdminInput('Price'.tr(), _prodPriceCtrl,
                        type: TextInputType.number),
                    if (_editingProductId == null)
                      _buildAdminInput('Store User ID'.tr(), _prodStoreUserIdCtrl),
                    Container(
                      margin: const EdgeInsets.only(bottom: 12),
                      child: DropdownButtonFormField<int>(
                        initialValue: _prodCategoryId,
                        decoration: InputDecoration(
                          labelText: 'Category'.tr(),
                          labelStyle:
                              TextStyle(color: theme.colorScheme.onSurfaceVariant),
                          filled: true,
                          fillColor: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                          border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: BorderSide(
                                  color: theme.dividerColor)),
                          enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: BorderSide(
                                  color: theme.dividerColor)),
                        ),
                        dropdownColor: theme.colorScheme.surface,
                        style: TextStyle(color: theme.colorScheme.onSurface),
                        items: _categories
                            .map((c) => DropdownMenuItem<int>(
                                value: c['id'] as int?,
                                child: Text(c['name'] ?? '')))
                            .toList(),
                        onChanged: (v) => setState(() {
                          _prodCategoryId = v;
                          _prodSubcategoryId = null;
                        }),
                      ),
                    ),
                    if (_prodCategoryId != null &&
                        (_subcategories[_prodCategoryId] ?? []).isNotEmpty)
                      Container(
                        margin: const EdgeInsets.only(bottom: 12),
                        child: DropdownButtonFormField<int>(
                          initialValue: _prodSubcategoryId,
                          decoration: InputDecoration(
                            labelText: 'Subcategory (Optional)'.tr(),
                            labelStyle:
                                TextStyle(color: theme.colorScheme.onSurfaceVariant),
                            filled: true,
                            fillColor: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
                            border: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(12),
                                borderSide: BorderSide(
                                    color: theme.dividerColor)),
                            enabledBorder: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(12),
                                borderSide: BorderSide(
                                    color: theme.dividerColor)),
                          ),
                          dropdownColor: theme.colorScheme.surface,
                          style: TextStyle(color: theme.colorScheme.onSurface),
                          items: [
                            DropdownMenuItem<int>(
                                value: null, child: Text('None'.tr())),
                            ...(_subcategories[_prodCategoryId]!).map((c) =>
                                DropdownMenuItem<int>(
                                    value: c['id'] as int?,
                                    child: Text(c['name'] ?? ''))),
                          ],
                          onChanged: (v) =>
                              setState(() => _prodSubcategoryId = v),
                        ),
                      ),
                    const SizedBox(height: 10),
                    if (_prodImageFile != null ||
                        (_prodImageUrl != null && _prodImageUrl!.isNotEmpty))
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: _prodImageFile != null
                              ? Image.file(_prodImageFile!,
                                  height: 80, width: 80, fit: BoxFit.cover)
                              : Image.network(
                                  _prodImageUrl!.startsWith('http')
                                      ? _prodImageUrl!
                                      : '${Api.baseUrl}$_prodImageUrl',
                                  height: 80,
                                  width: 80,
                                  fit: BoxFit.cover,
                                  errorBuilder: (_, __, ___) =>
                                      const SizedBox()),
                        ),
                      ),
                    OutlinedButton.icon(
                      onPressed: _pickProductImage,
                      icon: const Icon(Icons.image_rounded),
                      label: Text('Pick Product Image'.tr()),
                    ),
                    const SizedBox(height: 14),
                    FilledButton(
                      onPressed: _prodSubmitting ? null : _submitProduct,
                      child: Text(_prodSubmitting
                          ? 'Saving...'.tr()
                          : (_editingProductId != null ? 'Update'.tr() : 'Create'.tr())),
                    ),
                  ],
                ),
              ),
            ),
          ),
        if (!_showProductForm)
          Expanded(
            child: _loading
                ? Shimmer(
                    child: ListView.builder(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      itemCount: 5,
                      itemBuilder: (ctx, i) => Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14)),
                        child: const Padding(
                          padding: EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Skeleton(width: 150, height: 18),
                              SizedBox(height: 8),
                              Skeleton(width: 250, height: 14),
                              SizedBox(height: 12),
                              Row(children: [
                                Skeleton(
                                    width: 70,
                                    height: 28,
                                    borderRadius:
                                        BorderRadius.all(Radius.circular(8))),
                                SizedBox(width: 8),
                                Skeleton(
                                    width: 70,
                                    height: 28,
                                    borderRadius:
                                        BorderRadius.all(Radius.circular(8))),
                              ]),
                            ],
                          ),
                        ),
                      ),
                    ),
                  )
                : _products.isEmpty
                    ? Center(
                        child: Text(
                            'No {{status}} products found.'.tr(namedArgs: {'status': _statusFilter.toLowerCase()}),
                            style: TextStyle(
                                color: theme.colorScheme.onSurface
                                    .withValues(alpha: 0.5))))
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.builder(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          itemCount: _products.length,
                          itemBuilder: (ctx, i) =>
                              _buildProductCard(_products[i], theme),
                        ),
                      ),
          ),
      ],
    );
  }

  Widget _buildProductCard(Map<String, dynamic> p, ThemeData theme) {
    final status = p['status']?.toString() ?? 'Pending';
    final price = (p['price'] as num?)?.toDouble() ?? 0;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Expanded(
                  child: Text(p['name'] ?? '',
                      style: const TextStyle(
                          fontWeight: FontWeight.bold, fontSize: 16))),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                    color: _statusColor(status).withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20)),
                child: Text(status.tr(),
                    style: TextStyle(
                        color: _statusColor(status),
                        fontWeight: FontWeight.bold,
                        fontSize: 11)),
              ),
            ]),
            const SizedBox(height: 6),
            Text(
                '\$${price.toStringAsFixed(2)} • ${p['categoryName'] ?? ''} • ${'by'.tr()} ${p['storeName'] ?? p['storeUserId'] ?? ''}',
                style: TextStyle(
                    fontSize: 13,
                    color: theme.colorScheme.onSurface.withValues(alpha: 0.6))),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                if (status != 'Approved')
                  _actionBtn('Approve'.tr(), Colors.green,
                      () => _approveProduct(p['id'] as int)),
                if (status != 'Rejected')
                  _actionBtn('Reject'.tr(), Colors.red,
                      () => _rejectProduct(p['id'] as int)),
                _actionBtn('Edit'.tr(), Colors.blue, () => _startEditProduct(p)),
                _actionBtn(
                    'Delete'.tr(), Colors.red, () => _deleteProduct(p['id'] as int)),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _actionBtn(String text, Color color, VoidCallback onTap) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
        decoration: BoxDecoration(
          border: Border.all(color: color.withValues(alpha: 0.5)),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(text,
            style: TextStyle(
                color: color, fontSize: 12, fontWeight: FontWeight.bold)),
      ),
    );
  }

  // ══════════════ Users Tab ══════════════
  Widget _buildUsersTab(ThemeData theme) {
    if (_loading) {
      return Shimmer(
        child: ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: 6,
          itemBuilder: (ctx, i) => Card(
            margin: const EdgeInsets.only(bottom: 8),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            child: const ListTile(
              leading: Skeleton(width: 40, height: 40, shape: BoxShape.circle),
              title: Skeleton(width: 150, height: 16),
              subtitle: Skeleton(width: 100, height: 12),
            ),
          ),
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: _load,
      child: _users.isEmpty
          ? Center(
              child: Text('No users found.'.tr(),
                  style: TextStyle(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.5))))
          : ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: _users.length,
              itemBuilder: (ctx, i) {
                final u = _users[i];
                final isDisabled = u['isDisabled'] == true;
                return Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14)),
                  child: ListTile(
                    contentPadding:
                        const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                    leading: CircleAvatar(
                      backgroundColor: isDisabled
                          ? Colors.red.withValues(alpha: 0.15)
                          : theme.colorScheme.primary.withValues(alpha: 0.15),
                      child: Icon(
                          isDisabled
                              ? Icons.person_off_rounded
                              : Icons.person_rounded,
                          color: isDisabled
                              ? Colors.red
                              : theme.colorScheme.primary),
                    ),
                    title: Text(u['email'] ?? '',
                        style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text(
                        '${u['accountType'] ?? ''}${u['fullName'] != null ? ' • ${u['fullName']}' : ''}${isDisabled ? ' • ⛔ ${'Disabled'.tr()}' : ''}',
                        style: TextStyle(
                            fontSize: 12,
                            color:
                                theme.colorScheme.onSurface.withValues(alpha: 0.6))),
                    trailing: FilledButton.tonal(
                      onPressed: () =>
                          _toggleUser(u['id'].toString(), isDisabled),
                      style: FilledButton.styleFrom(
                        backgroundColor: isDisabled
                            ? Colors.green.withValues(alpha: 0.12)
                            : Colors.red.withValues(alpha: 0.12),
                        foregroundColor: isDisabled ? Colors.green : Colors.red,
                        padding: const EdgeInsets.symmetric(horizontal: 14),
                      ),
                      child: Text(isDisabled ? 'Enable'.tr() : 'Disable'.tr(),
                          style: const TextStyle(fontSize: 12)),
                    ),
                  ),
                );
              },
            ),
    );
  }

  // ══════════════ Orders Tab ══════════════
  Widget _buildOrdersTab(ThemeData theme) {
    if (_loading) {
      return Shimmer(
        child: ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: 6,
          itemBuilder: (ctx, i) => Card(
            margin: const EdgeInsets.only(bottom: 12),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            child: const Padding(
              padding: EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Skeleton(width: 100, height: 18),
                        Skeleton(
                            width: 60,
                            height: 20,
                            borderRadius:
                                BorderRadius.all(Radius.circular(10))),
                      ]),
                  SizedBox(height: 12),
                  Skeleton(width: 200, height: 14),
                ],
              ),
            ),
          ),
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: _load,
      child: _orders.isEmpty
          ? Center(
              child: Text('No orders yet.'.tr(),
                  style: TextStyle(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.5))))
          : ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: _orders.length,
              itemBuilder: (ctx, i) {
                final o = _orders[i];
                final total = (o['totalAmount'] as num?)?.toDouble() ?? 0;
                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14)),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                  child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text('${'Order'.tr()} #${o['id']}',
                                    style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                        fontSize: 16)),
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 10, vertical: 4),
                                  decoration: BoxDecoration(
                                      color: theme.colorScheme.primary
                                          .withValues(alpha: 0.12),
                                      borderRadius: BorderRadius.circular(20)),
                                  child: Text((o['status'] ?? '').toString().tr(),
                                      style: TextStyle(
                                          color: theme.colorScheme.primary,
                                          fontWeight: FontWeight.bold,
                                          fontSize: 11)),
                                ),
                              ]),
                          const SizedBox(height: 8),
                          Text(
                              '${'User:'.tr()} ${o['userId'] ?? ''}\n${'Store:'.tr()} ${o['storeName'] ?? o['storeUserId'] ?? ''}\n${'Total:'.tr()} \$${total.toStringAsFixed(2)}',
                              style: TextStyle(
                                  fontSize: 13,
                                  color: theme.colorScheme.onSurface
                                      .withValues(alpha: 0.6))),
                        ]),
                  ),
                );
              },
            ),
    );
  }

  // ══════════════ Categories Tab ══════════════
  Widget _buildCategoriesTab(ThemeData theme) {
    if (_loading) {
      return Shimmer(
        child: ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: 5,
          itemBuilder: (ctx, i) => Card(
            margin: const EdgeInsets.only(bottom: 12),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            child: const Padding(
              padding: EdgeInsets.all(16),
              child: Row(
                children: [
                  Skeleton(
                      width: 48,
                      height: 48,
                      borderRadius: BorderRadius.all(Radius.circular(8))),
                  SizedBox(width: 12),
                  Skeleton(width: 150, height: 16),
                ],
              ),
            ),
          ),
        ),
      );
    }
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Create New Category'.tr(),
                    style:
                        TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                const SizedBox(height: 12),
                TextField(
                    controller: _catNameCtrl,
                    decoration: InputDecoration(
                        labelText: 'Name'.tr(), border: const OutlineInputBorder())),
                const SizedBox(height: 10),
                TextField(
                    controller: _catCodeCtrl,
                    decoration: InputDecoration(
                        labelText: 'Unique Code (optional)'.tr(),
                        border: const OutlineInputBorder())),
                const SizedBox(height: 10),
                if (_catParentId == null) ...[
                  if (_catImageFile != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.file(_catImageFile!,
                              height: 80, width: 80, fit: BoxFit.cover)),
                    ),
                  OutlinedButton.icon(
                    onPressed: () async {
                      final picked = await ImagePicker().pickImage(
                          source: ImageSource.gallery, imageQuality: 80);
                      if (picked != null) {
                        setState(() => _catImageFile = File(picked.path));
                      }
                    },
                    icon: const Icon(Icons.image_rounded),
                    label: Text(_catImageFile != null
                        ? 'Change Image'.tr()
                        : 'Pick Category Image'.tr()),
                  ),
                ],
                const SizedBox(height: 10),
                DropdownButtonFormField<int>(
                  initialValue: _catParentId,
                  decoration: InputDecoration(
                      labelText: 'Parent Category (Optional)'.tr(),
                      border: const OutlineInputBorder()),
                  items: [
                    DropdownMenuItem<int>(
                        value: null, child: Text('None (Top-level)'.tr())),
                    ..._categories.map((c) => DropdownMenuItem<int>(
                        value: c['id'] as int?, child: Text(c['name'] ?? ''))),
                  ],
                  onChanged: (v) => setState(() {
                    _catParentId = v;
                    if (v != null) _catImageFile = null;
                  }),
                ),
                const SizedBox(height: 14),
                FilledButton(
                    onPressed: _creating ? null : _createCategory,
                    child: Text(_creating ? 'Creating...'.tr() : 'Create Category'.tr())),
              ],
            ),
          ),
        ),
        const SizedBox(height: 20),
        Text('Existing Categories'.tr(),
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
        const SizedBox(height: 12),
        if (_categories.isEmpty)
          Center(child: Text('No categories found.'.tr()))
        else
          ..._categories.map((c) {
            final subs = _subcategories[c['id']] ?? [];
            final imgUrl = c['imageUrl']?.toString();
            return Card(
              margin: const EdgeInsets.only(bottom: 8),
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14)),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(children: [
                      if (imgUrl != null && imgUrl.isNotEmpty)
                        ClipRRect(
                          borderRadius: BorderRadius.circular(8),
                          child: Image.network(
                            imgUrl.startsWith('http')
                                ? imgUrl
                                : '${Api.baseUrl}$imgUrl',
                            width: 48,
                            height: 48,
                            fit: BoxFit.cover,
                            errorBuilder: (_, __, ___) => Container(
                                width: 48,
                                height: 48,
                                color: Colors.grey.shade200,
                                child: const Icon(Icons.image,
                                    color: Colors.grey)),
                          ),
                        )
                      else
                        Container(
                            width: 48,
                            height: 48,
                            decoration: BoxDecoration(
                                color: Colors.grey.shade200,
                                borderRadius: BorderRadius.circular(8)),
                            child: const Icon(Icons.image, color: Colors.grey)),
                      const SizedBox(width: 12),
                      Expanded(
                          child: Text('${c['name'] ?? ''} (${c['code'] ?? ''})',
                              style: const TextStyle(
                                  fontWeight: FontWeight.w600, fontSize: 16))),
                      PopupMenuButton<String>(
                        onSelected: (val) {
                          if (val == 'change_image') {
                            _changeCategoryImage(c['id'] as int);
                          }
                          if (val == 'remove_image') {
                            _removeCategoryImage(c['id'] as int);
                          }
                          if (val == 'delete') _deleteCategory(c['id'] as int);
                        },
                        itemBuilder: (_) => [
                          PopupMenuItem(
                              value: 'change_image',
                              child: Text('Change Image'.tr())),
                          if (imgUrl != null && imgUrl.isNotEmpty)
                            PopupMenuItem(
                                value: 'remove_image',
                                child: Text('Remove Image'.tr())),
                          PopupMenuItem(
                              value: 'delete', child: Text('Delete Category'.tr())),
                        ],
                      ),
                    ]),
                    if (subs.isNotEmpty) ...[
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        runSpacing: 4,
                        children: subs
                            .map((sub) => Chip(
                                  label: Text(sub['name'] ?? '',
                                      style: const TextStyle(fontSize: 12)),
                                  padding: EdgeInsets.zero,
                                  materialTapTargetSize:
                                      MaterialTapTargetSize.shrinkWrap,
                                  deleteIcon: const Icon(Icons.close, size: 16),
                                  onDeleted: () =>
                                      _deleteCategory(sub['id'] as int),
                                ))
                            .toList(),
                      ),
                    ]
                  ],
                ),
              ),
            );
          }),
      ],
    );
  }

  // ══════════════ Ads Tab ══════════════
  Widget _buildAdsTab(ThemeData theme) {
    if (_loading) {
      return Shimmer(
        child: ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: 4,
          itemBuilder: (ctx, i) => Card(
            margin: const EdgeInsets.only(bottom: 12),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            child: const ListTile(
              leading: Skeleton(
                  width: 80,
                  height: 50,
                  borderRadius: BorderRadius.all(Radius.circular(8))),
              title: Skeleton(width: 120, height: 16),
              subtitle: Skeleton(width: 80, height: 12),
            ),
          ),
        ),
      );
    }
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Add New Ad Banner'.tr(),
                    style:
                        TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                const SizedBox(height: 12),
                TextField(
                    controller: _adTitleCtrl,
                    decoration: InputDecoration(
                        labelText: 'Title (optional)'.tr(),
                        border: const OutlineInputBorder())),
                const SizedBox(height: 10),
                TextField(
                    controller: _adLinkUrlCtrl,
                    decoration: InputDecoration(
                        labelText: 'Link URL (opens when clicked)'.tr(),
                        border: const OutlineInputBorder())),
                const SizedBox(height: 10),
                TextField(
                    controller: _adSortOrderCtrl,
                    decoration: InputDecoration(
                        labelText: 'Sort Order'.tr(), border: const OutlineInputBorder()),
                    keyboardType: TextInputType.number),
                const SizedBox(height: 10),
                if (_adImageFile != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Image.file(_adImageFile!,
                          height: 100,
                          width: double.infinity,
                          fit: BoxFit.cover),
                    ),
                  ),
                OutlinedButton.icon(
                  onPressed: () async {
                    final picked = await ImagePicker().pickImage(
                        source: ImageSource.gallery, imageQuality: 80);
                    if (picked != null) {
                      setState(() => _adImageFile = File(picked.path));
                    }
                  },
                  icon: const Icon(Icons.image_rounded),
                  label: Text(_adImageFile != null
                      ? 'Change Image'.tr()
                      : 'Pick Banner Image *'.tr()),
                ),
                const SizedBox(height: 14),
                FilledButton(
                    onPressed: _adCreating ? null : _createAd,
                    child: Text(
                        _adCreating ? 'Uploading...'.tr() : 'Create Ad Banner'.tr())),
              ],
            ),
          ),
        ),
        const SizedBox(height: 20),
        Text('Existing Ad Banners'.tr(),
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
        const SizedBox(height: 12),
        if (_ads.isEmpty)
          Center(child: Text('No ad banners yet.'.tr()))
        else
          ..._ads.map((ad) {
            final imgUrl = ad['imageUrl']?.toString() ?? '';
            return Card(
              margin: const EdgeInsets.only(bottom: 12),
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14)),
              child: ListTile(
                contentPadding: const EdgeInsets.all(12),
                leading: ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.network(
                    '${imgUrl.startsWith('http')
                            ? imgUrl
                            : '${Api.baseUrl}$imgUrl'}?t=${DateTime.now().millisecondsSinceEpoch}',
                    width: 80,
                    height: 50,
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => Container(
                        width: 80, height: 50, color: Colors.grey.shade300),
                  ),
                ),
                title: Text(ad['title']?.toString() ?? '(No title)'.tr(),
                    style: const TextStyle(fontWeight: FontWeight.w600)),
                subtitle: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (ad['linkUrl'] != null &&
                        ad['linkUrl'].toString().isNotEmpty)
                      Text(ad['linkUrl'].toString(),
                          style: TextStyle(
                              fontSize: 11, color: theme.colorScheme.primary)),
                    Text('${'Order:'.tr()} ${ad['sortOrder'] ?? 0}',
                        style:
                            const TextStyle(fontSize: 11, color: Colors.grey)),
                  ],
                ),
                trailing: IconButton(
                  icon:
                      const Icon(Icons.delete_rounded, color: Colors.redAccent),
                  onPressed: () => _deleteAd(ad['id'] as int),
                ),
              ),
            );
          }),
      ],
    );
  }

  Widget _buildAdminInput(String label, TextEditingController ctrl,
      {int maxLines = 1, TextInputType? type}) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextField(
        controller: ctrl,
        maxLines: maxLines,
        keyboardType: type,
        style: TextStyle(color: theme.colorScheme.onSurface),
        decoration: InputDecoration(
          labelText: label,
          labelStyle: TextStyle(color: theme.colorScheme.onSurfaceVariant),
          filled: true,
          fillColor: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
          border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide(color: theme.dividerColor)),
          enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide(color: theme.dividerColor)),
        ),
      ),
    );
  }
}

