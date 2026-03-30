using System;
using System.Threading.Tasks;
using EWMS_WPF.Models;

namespace EWMS_WPF.DAL
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<Warehouse> Warehouses { get; }
        IGenericRepository<UserWarehouse> UserWarehouses { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<ProductCategory> ProductCategories { get; }
        IGenericRepository<Supplier> Suppliers { get; }
        IGenericRepository<Location> Locations { get; }
        IGenericRepository<Inventory> Inventories { get; }
        IGenericRepository<PurchaseOrder> PurchaseOrders { get; }
        IGenericRepository<PurchaseOrderDetail> PurchaseOrderDetails { get; }
        IGenericRepository<SalesOrder> SalesOrders { get; }
        IGenericRepository<SalesOrderDetail> SalesOrderDetails { get; }
        IGenericRepository<StockInReceipt> StockInReceipts { get; }
        IGenericRepository<StockInDetail> StockInDetails { get; }
        IGenericRepository<StockOutReceipt> StockOutReceipts { get; }
        IGenericRepository<StockOutDetail> StockOutDetails { get; }
        
        Task<int> SaveChangesAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly EWMSDbContext _context;

        private IGenericRepository<User>? _users;
        private IGenericRepository<Role>? _roles;
        private IGenericRepository<Warehouse>? _warehouses;
        private IGenericRepository<UserWarehouse>? _userWarehouses;
        private IGenericRepository<Product>? _products;
        private IGenericRepository<ProductCategory>? _productCategories;
        private IGenericRepository<Supplier>? _suppliers;
        private IGenericRepository<Location>? _locations;
        private IGenericRepository<Inventory>? _inventories;
        private IGenericRepository<PurchaseOrder>? _purchaseOrders;
        private IGenericRepository<PurchaseOrderDetail>? _purchaseOrderDetails;
        private IGenericRepository<SalesOrder>? _salesOrders;
        private IGenericRepository<SalesOrderDetail>? _salesOrderDetails;
        private IGenericRepository<StockInReceipt>? _stockInReceipts;
        private IGenericRepository<StockInDetail>? _stockInDetails;
        private IGenericRepository<StockOutReceipt>? _stockOutReceipts;
        private IGenericRepository<StockOutDetail>? _stockOutDetails;

        public UnitOfWork(EWMSDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
        public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);
        public IGenericRepository<Warehouse> Warehouses => _warehouses ??= new GenericRepository<Warehouse>(_context);
        public IGenericRepository<UserWarehouse> UserWarehouses => _userWarehouses ??= new GenericRepository<UserWarehouse>(_context);
        public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);
        public IGenericRepository<ProductCategory> ProductCategories => _productCategories ??= new GenericRepository<ProductCategory>(_context);
        public IGenericRepository<Supplier> Suppliers => _suppliers ??= new GenericRepository<Supplier>(_context);
        public IGenericRepository<Location> Locations => _locations ??= new GenericRepository<Location>(_context);
        public IGenericRepository<Inventory> Inventories => _inventories ??= new GenericRepository<Inventory>(_context);
        public IGenericRepository<PurchaseOrder> PurchaseOrders => _purchaseOrders ??= new GenericRepository<PurchaseOrder>(_context);
        public IGenericRepository<PurchaseOrderDetail> PurchaseOrderDetails => _purchaseOrderDetails ??= new GenericRepository<PurchaseOrderDetail>(_context);
        public IGenericRepository<SalesOrder> SalesOrders => _salesOrders ??= new GenericRepository<SalesOrder>(_context);
        public IGenericRepository<SalesOrderDetail> SalesOrderDetails => _salesOrderDetails ??= new GenericRepository<SalesOrderDetail>(_context);
        public IGenericRepository<StockInReceipt> StockInReceipts => _stockInReceipts ??= new GenericRepository<StockInReceipt>(_context);
        public IGenericRepository<StockInDetail> StockInDetails => _stockInDetails ??= new GenericRepository<StockInDetail>(_context);
        public IGenericRepository<StockOutReceipt> StockOutReceipts => _stockOutReceipts ??= new GenericRepository<StockOutReceipt>(_context);
        public IGenericRepository<StockOutDetail> StockOutDetails => _stockOutDetails ??= new GenericRepository<StockOutDetail>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
