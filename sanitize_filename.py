import os
import re
import logging
from pathlib import Path

def setup_logging():
    """Set up logging configuration."""
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler('rename_files.log'),
            logging.StreamHandler()
        ]
    )

def is_valid_windows_filename(filename):
    """Check if the filename is valid for Windows."""
    # Windows reserved names (case-insensitive)
    reserved_names = {
        'CON', 'PRN', 'AUX', 'NUL', 'COM1', 'COM2', 'COM3', 'COM4', 'COM5',
        'COM6', 'COM7', 'COM8', 'COM9', 'LPT1', 'LPT2', 'LPT3', 'LPT4',
        'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9'
    }
    
    # Check for reserved names
    name_without_ext = os.path.splitext(filename)[0].upper()
    if name_without_ext in reserved_names:
        return False
    
    # Check for trailing spaces or dots
    if filename.endswith(' ') or filename.endswith('.'):
        return False
    
    # Check if filename contains only allowed characters (alphanumeric, underscore, hyphen, period)
    allowed_pattern = r'^[a-zA-Z0-9_\-\.]+$'
    if not re.match(allowed_pattern, filename):
        return False
        
    return True

def sanitize_filename(filename, replacement='_'):
    """Replace non-alphanumeric characters (except _, -, .) with a specified character."""
    # Split into name and extension
    name, ext = os.path.splitext(filename)
    
    # Replace non-alphanumeric characters (keep _, -)
    sanitized_name = re.sub(r'[^a-zA-Z0-9_\-]', replacement, name)
    
    # Remove trailing spaces or dots from name
    sanitized_name = sanitized_name.strip(' .')
    
    # If the name is empty after sanitization, provide a default
    if not sanitized_name:
        sanitized_name = 'unnamed_file'
    
    # Reattach the extension (keep it as is, but ensure no invalid chars)
    sanitized_ext = re.sub(r'[^a-zA-Z0-9\.]', replacement, ext) if ext else ''
    sanitized = sanitized_name + sanitized_ext
    
    return sanitized

def rename_files_in_directory(directory):
    """Recursively rename files and folders in the given directory to ensure Windows-compatible names."""
    directory = Path(directory).resolve()
    
    if not directory.exists():
        logging.error(f"Directory {directory} does not exist.")
        return
    
    # Walk through directory recursively, bottom-up
    for root, dirs, files in os.walk(directory, topdown=False):
        # Process files
        for old_name in files:
            old_path = Path(root) / old_name
            if not is_valid_windows_filename(old_name):
                new_name = sanitize_filename(old_name)
                new_path = Path(root) / new_name
                
                # Avoid overwriting existing files
                counter = 1
                base_name, ext = os.path.splitext(new_name)
                while new_path.exists():
                    new_name = f"{base_name}_{counter}{ext}"
                    new_path = Path(root) / new_name
                    counter += 1
                
                try:
                    old_path.rename(new_path)
                    logging.info(f"Renamed file: {old_path} -> {new_path}")
                except OSError as e:
                    logging.error(f"Failed to rename {old_path} to {new_path}: {e}")
        
        # Process directories
        for old_name in dirs:
            old_path = Path(root) / old_name
            if not is_valid_windows_filename(old_name):
                new_name = sanitize_filename(old_name)
                new_path = Path(root) / new_name
                
                # Avoid overwriting existing directories
                counter = 1
                base_name = new_name
                while new_path.exists():
                    new_name = f"{base_name}_{counter}"
                    new_path = Path(root) / new_name
                    counter += 1
                
                try:
                    old_path.rename(new_path)
                    logging.info(f"Renamed directory: {old_path} -> {new_path}")
                except OSError as e:
                    logging.error(f"Failed to rename {old_path} to {new_path}: {e}")

def main():
    """Main function to run the script."""
    setup_logging()
    
    # Specify the directory to process
    directory = input("Enter the directory path to process: ").strip()
    
    if not directory:
        logging.error("No directory provided.")
        return
    
    logging.info(f"Processing directory: {directory}")
    rename_files_in_directory(directory)
    logging.info("Processing complete.")

if __name__ == "__main__":
    main()
