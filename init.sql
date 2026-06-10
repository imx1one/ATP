-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema atp_db
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema atp_db
-- -----------------------------------------------------

CREATE SCHEMA IF NOT EXISTS `atp_db` DEFAULT CHARACTER SET utf8mb4;
USE `atp_db` ;

-- -----------------------------------------------------
-- Table `atp_db`.`drivers`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `atp_db`.`drivers` (
  `iddrivers` INT NOT NULL AUTO_INCREMENT,
  `full_name` VARCHAR(100) NOT NULL,
  `license` VARCHAR(20) NULL DEFAULT NULL,
  `license_expiry` DATE NULL DEFAULT NULL,
  `phone` VARCHAR(20) NULL DEFAULT NULL,
  `status` VARCHAR(20) NULL DEFAULT 'available',
  PRIMARY KEY (`iddrivers`),
  UNIQUE INDEX `license_UNIQUE` (`license` ASC) VISIBLE)
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4;


-- -----------------------------------------------------
-- Table `atp_db`.`vehicles`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `atp_db`.`vehicles` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `plate_number` VARCHAR(45) NOT NULL,
  `brand` VARCHAR(50) NULL DEFAULT NULL,
  `model` VARCHAR(50) NULL DEFAULT NULL,
  `year` INT NULL DEFAULT NULL,
  `mileage` INT NULL DEFAULT '0',
  `status` VARCHAR(20) NULL DEFAULT 'active',
  PRIMARY KEY (`id`),
  UNIQUE INDEX `plate_number_UNIQUE` (`plate_number` ASC) VISIBLE)
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4;


-- -----------------------------------------------------
-- Table `atp_db`.`fuel_logs`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `atp_db`.`fuel_logs` (
  `idfuel_logs` INT NOT NULL AUTO_INCREMENT,
  `vehicles_id` INT NOT NULL,
  `drivers_id` INT NOT NULL,
  `fuel_date` DATE NOT NULL,
  `liters` DECIMAL(5,2) NOT NULL,
  `cost_total` DECIMAL(10,2) NOT NULL,
  `odometer` INT NOT NULL,
  PRIMARY KEY (`idfuel_logs`, `vehicles_id`, `drivers_id`),
  INDEX `fk_fuel_logs_vehicles1_idx` (`vehicles_id` ASC) VISIBLE,
  INDEX `fk_fuel_logs_drivers1_idx` (`drivers_id` ASC) VISIBLE,
  CONSTRAINT `fk_fuel_logs_drivers1`
    FOREIGN KEY (`drivers_id`)
    REFERENCES `atp_db`.`drivers` (`iddrivers`),
  CONSTRAINT `fk_fuel_logs_vehicles1`
    FOREIGN KEY (`vehicles_id`)
    REFERENCES `atp_db`.`vehicles` (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 7
DEFAULT CHARACTER SET = utf8mb4;


-- -----------------------------------------------------
-- Table `atp_db`.`maintanance`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `atp_db`.`maintanance` (
  `idmaintanance` INT NOT NULL AUTO_INCREMENT,
  `vehicles_id` INT NOT NULL,
  `service_date` DATE NOT NULL,
  `mileage_at_service` INT NOT NULL,
  `service_type` VARCHAR(50) NULL DEFAULT NULL,
  `cost` DECIMAL(10,2) NULL DEFAULT '0.00',
  `next_due_date` DATE NULL DEFAULT NULL,
  `description` TEXT NULL DEFAULT NULL,
  PRIMARY KEY (`idmaintanance`, `vehicles_id`),
  INDEX `fk_maintanance_vehicles_idx` (`vehicles_id` ASC) VISIBLE,
  CONSTRAINT `fk_maintanance_vehicles`
    FOREIGN KEY (`vehicles_id`)
    REFERENCES `atp_db`.`vehicles` (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;

ALTER TABLE vehicles AUTO_INCREMENT = 1;
ALTER TABLE drivers AUTO_INCREMENT = 1;
ALTER TABLE fuel_logs AUTO_INCREMENT = 1;
ALTER TABLE maintanance AUTO_INCREMENT = 1;

insert into vehicles (plate_number, brand, model, year, mileage, status) values
('У118КС73', 'SUBARU', 'IMPREZA', 2008, 280000, 'active'),
('А415УУ172', 'AUDI', 'A3', 2014, 150000, 'active');

insert into drivers (full_name, license, license_expiry, phone, status) values
('Латыпов А.А.', '99AA123456', '2027-05-15', '+79001112233', 'available'),
('Михно Н.А.', '99BB654321', '2026-11-20', '+79004445566', 'available');

insert into maintanance (vehicles_id, service_date, mileage_at_service, service_type, cost, next_due_date, description) values
(1, '2024-01-15', 200000, 'ТО-2', 8500.00, '2025-07-15', 'Замена масла, фильтров, тормозных колодок'),
(2, '2024-03-10', 100000, 'Замена масла', 3200.00, '2025-09-10', 'Плановая замена');

insert into fuel_logs (vehicles_id, drivers_id, fuel_date, liters, cost_total, odometer) values
(1, 1, '2026-02-01', 45.50, 2730.00, 260000),
(2, 2, '2025-02-05', 38.20, 2292.00, 140000);


