/**
 * =====================================================================================
 * C# CONCEPTS EXPLAINED FOR STUDENTS
 * =====================================================================================
 * 
 * 1. WHAT IS "namespace"?
 *    A namespace is like a folder for code. It groups related classes together
 *    so you don't have naming conflicts. Think of it like last names in a phonebook -
 *    "John Smith" might appear twice, but "John Smith from Boston" and 
 *    "John Smith from New York" are different because of the namespace.
 * 
 *    Example:
 *      namespace VenuePlatform.BLL.Domain.Spaces
 *      {
 *          // All classes in here are in the "Spaces" namespace
 *      }
 * 
 * 2. WHAT IS "public sealed class"?
 *    - "public" = Anyone can use this class
 *    - "sealed" = This class CANNOT be inherited (no subclass can extend it)
 *    - "class" = A blueprint for creating objects
 * 
 *    What's the difference between class and sealed class?
 *    - Regular class can be inherited (extended)
 *    - Sealed class cannot be inherited - it's the "final form"
 * 
 * 3. WHAT IS ": ITenantScoped"?
 *    This means the Space class "implements" the ITenantScoped interface.
 *    It's like signing a contract - the class promises to provide certain functionality.
 *    In this case, it promises to have a CompanyId property (for multi-tenancy).
 * 
 *    Think of it like a job interview:
 *    - Interface = The job requirements
 *    - Class = The person who gets the job
 *    - The person must meet all requirements (implement all interface members)
 * 
 * 4. WHAT IS "Guid"?
 *    Guid stands for "Globally Unique Identifier" - a 128-bit number that is
 *    virtually guaranteed to be unique. It's like a fingerprint for data.
 *    Generated with Guid.NewGuid()
 * 
 *    Example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
 * 
 * 5. WHAT IS "private set"?
 *    This is a C# property with a private setter.
 *    - "get" allows reading the value (from anywhere)
 *    - "private set" allows changing the value ONLY from within this class
 * 
 *    This is part of "encapsulation" - protecting data from being changed
 *    incorrectly from outside the class.
 * 
 *    Example:
 *      public Guid Id { get; private set; }
 *      // Anyone can READ Id, but only code INSIDE this class can SET it
 * 
 * 6. WHAT IS "string?" (nullable reference type)?
 *    In C# 8.0+, adding "?" after a reference type makes it nullable.
 *    - string = Must have a value (non-nullable)
 *    - string? = Can be null (nullable)
 * 
 *    Example:
 *      public string Name { get; private set; }     // Cannot be null
 *      public string? Notes { get; private set; }  // Can be null
 * 
 * 7. WHAT IS "= null!"?
 *    This is a "null-forgiving operator". It tells the compiler:
 *    "I know this looks like it could be null, but trust me, it's initialized
 *    before any code tries to use it."
 * 
 *    It's used for EF Core navigation properties that are loaded from the database
 *    but can't be set in the constructor.
 * 
 * 8. WHAT IS "constructor"?
 *    A special method that runs when creating a new instance of a class.
 *    It's named after the class and has no return type.
 * 
 *    Example:
 *      public Space(Guid companyId, string name, int capacity)
 *      {
 *          // This is the constructor - runs when you do "new Space(...)"
 *      }
 * 
 * 9. WHAT IS "throw new ArgumentException"?
 *    This is how we validate input in C#. If someone passes bad data,
 *    we "throw" an exception which stops the program and shows an error.
 * 
 *    Example:
 *      if (string.IsNullOrWhiteSpace(name))
 *          throw new ArgumentException("Name is required.", nameof(name));
 * 
 * 10. WHAT IS "nameof(name)"?
 *     This gets the string name of a variable. If the variable is called "name",
 *     nameof(name) returns "name".
 * 
 *     Why use this? If you rename the variable, the error message automatically
 *     updates too!
 * 
 * 11. WHAT IS "Guid.NewGuid()"?
 *     This generates a new unique identifier for the object.
 *     Every time you create a Space, it gets its own unique ID.
 * 
 * 12. WHAT IS "DateTime.UtcNow"?
 *     Gets the current date and time in UTC (Coordinated Universal Time).
 *     Using UTC prevents timezone issues when storing/retrieving dates.
 * 
 * 13. WHAT IS "=>" (expression-bodied member)?
 *     This is shorthand for a simple method body.
 * 
 *     Traditional:
 *       public void Deactivate() { IsActive = false; }
 * 
 *     Expression-bodied:
 *       public void Deactivate() => IsActive = false;
 * 
 * 14. WHAT IS "ITenantScoped"?
 *     This is an interface that marks entities as belonging to a specific company.
 *     It ensures every Space has a CompanyId for multi-tenancy (separating data
 *     between different companies using the same system).
 * 
 * =====================================================================================
 */

using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Spaces;

/**
 * Space - A room or venue that can be booked
 * 
 * This is one of the core entities in our system. A Space represents
 * a physical room or area that customers can book for events.
 * 
 * Key properties:
 * - Id: Unique identifier (Guid)
 * - CompanyId: Which company owns this space (for multi-tenancy)
 * - Name: Display name like "Conference Room A"
 * - Capacity: Maximum number of people
 * - HourlyRate: Price per hour
 * - Notes: Optional additional information
 * - IsActive: Whether the space is available for new bookings
 * - CreatedUtc: When the space was created
 */
public sealed class Space : ITenantScoped
{
    /// <summary>
    /// Unique identifier for this space
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Which company this space belongs to (for multi-tenancy)
    /// </summary>
    public Guid CompanyId { get; private set; }
    
    /// <summary>
    /// Display name of the space (e.g., "Conference Room A")
    /// </summary>
    public string Name { get; private set; } = null!;
    
    /// <summary>
    /// Maximum number of people the space can accommodate
    /// </summary>
    public int Capacity { get; private set; }
    
    /// <summary>
    /// Price per hour for booking this space
    /// </summary>
    public decimal HourlyRate { get; private set; }
    
    /// <summary>
    /// Optional notes about the space (can be null)
    /// </summary>
    public string? Notes { get; private set; }
    
    /// <summary>
    /// Is this space available for new bookings?
    /// False means it's been deactivated (soft delete)
    /// </summary>
    public bool IsActive { get; private set; }
    
    /// <summary>
    /// When this space was created (in UTC)
    /// </summary>
    public DateTime CreatedUtc { get; private set; }

    /// <summary>
    /// Empty constructor required by Entity Framework Core
    /// EF needs this to create objects when loading from database
    /// </summary>
    private Space() { } // EF

    /**
     * Constructor - Creates a new Space
     * 
     * @param companyId - The company that owns this space
     * @param name - Display name
     * @param capacity - Maximum attendees
     * @param hourlyRate - Price per hour
     * @param notes - Optional notes (can be null)
     * 
     * This constructor validates all input and throws exceptions if invalid.
     * This prevents creating invalid Space objects.
     */
    public Space(Guid companyId, string name, int capacity, decimal hourlyRate, string? notes = null)
    {
        // Validate input - throw exceptions if invalid
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.", nameof(capacity));
        if (hourlyRate < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));

        // Initialize the space with generated ID and current timestamp
        Id = Guid.NewGuid();  // Generate unique ID
        CompanyId = companyId;
        Name = name.Trim();   // Remove leading/trailing whitespace
        Capacity = capacity;
        HourlyRate = hourlyRate;
        Notes = notes;
        IsActive = true;      // New spaces are active by default
        CreatedUtc = DateTime.UtcNow;  // Current UTC time
    }

    /**
     * UpdateName - Changes the space's name
     * 
     * Validates that the new name is not empty or whitespace
     */
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
    }

    /**
     * UpdateCapacity - Changes the space's capacity
     * 
     * Validates that capacity is greater than 0
     */
    public void UpdateCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.", nameof(capacity));
        Capacity = capacity;
    }

    /**
     * UpdateHourlyRate - Changes the hourly rate
     * 
     * Validates that rate is not negative
     */
    public void UpdateHourlyRate(decimal hourlyRate)
    {
        if (hourlyRate < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));
        HourlyRate = hourlyRate;
    }

    /**
     * SetNotes - Updates the notes field
     * 
     * Can be called with null to clear notes
     */
    public void SetNotes(string? notes) => Notes = notes;

    /**
     * Deactivate - Soft deletes the space
     * 
     * Instead of actually deleting, we just mark it as inactive.
     * This preserves historical data (bookings, invoices, etc.)
     * while preventing new bookings.
     */
    public void Deactivate() => IsActive = false;
}
