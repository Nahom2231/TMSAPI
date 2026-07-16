using System;
using System.Collections.Generic;

namespace TmsApi.Domain.Entities;

public class Student
{
private readonly List<GradeRecord> _grades = new();

    public int Id { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    
    public int Age { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsDeleted {get; set; }
 
     public IReadOnlyCollection<GradeRecord> Grades => _grades.AsReadOnly();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Certificate> Certificates {get; set; } = new List<Certificate>();
    
    public uint Version { get; set; } 

    public Student() { }

    public Student(string registrationNumber, string name)
    {
        if(string.IsNullOrWhiteSpace(name ))
        {
            throw new ArgumentException("Student name cannot be empty or whitespace.", nameof(name));
        }
        RegistrationNumber = registrationNumber;
        Name = name;
    }

    public void AddGrade(GradeRecord grade)=> _grades.Add(grade);
}



